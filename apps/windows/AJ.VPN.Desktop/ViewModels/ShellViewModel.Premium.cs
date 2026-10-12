using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AJ.VPN.Application;
using AJ.VPN.Domain;
namespace AJ.VPN.Desktop.ViewModels;

public sealed partial class ShellViewModel
{
    private CancellationTokenSource? ipCancellation, testCancellation;
    private Task? testOperation;
    private string publicIp = "—", search = "";
    private long? connectedAt;
    private DispatcherTimer? durationTimer;
    private ServerFilter serverFilter;
    private ServerSort serverSort = ServerSort.LowestLatency;
    private bool favoritesFirst = true, testing;
    private string? resolvedServerId;
    private string libraryScope = "";
    private bool fullScopeFailed;
    private int testTotal, testCompleted, testHealthy, testFailed;
    public string? EditingSubscriptionId { get; set; }
    public string PublicIp { get => publicIp; private set { Set(ref publicIp, value); ipVerifiedAt = value == "—" ? null : DateTimeOffset.UtcNow; Raise(nameof(HasPublicIp)); Raise(nameof(PublicIpDisplay)); Raise(nameof(IpVerificationDisplay)); } }
    public bool HasPublicIp => PublicIp != "—";
    public bool NeedsSubscription => Subscriptions.Count == 0;
    public bool HasSubscriptions => Subscriptions.Count > 0;
    public bool NeedsServer => Servers.Count == 0;
    public bool CanProbe => State == ConnectionState.Connected && !Busy;
    public string ConsumerStatus => State == ConnectionState.Connected ? Resource(InternetVerified ? "InternetVerified" : "CoreReadyLabel") : Resource(State == ConnectionState.Error ? "ConnectionError" : State.ToString());
    public string Protocol => DisplayServer?.Protocol.ToUpperInvariant() ?? "—";
    public string Location => DisplayServer?.Country ?? Resource("UnknownLocation");
    public string ConnectionMode => proxy.IsApplied ? Resource("SystemProxyMode") : Resource("LocalProxyMode");
    public string TrafficRoutingSummary => Resource(proxy.IsApplied ? "ProxyTrafficEnabled" : "ProxyTrafficOff");
    public string Duration => connectedAt is { } start && State == ConnectionState.Connected ? FormatDuration(System.Diagnostics.Stopwatch.GetElapsedTime(start)) : "—";
    private static string FormatDuration(TimeSpan elapsed) => $"{(long)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    public ICollectionView ServerView { get; private set; } = null!;
    public string Search { get => search; set { if (Set(ref search, value)) ServerView.Refresh(); } }
    public bool FavoritesFirst { get => favoritesFirst; set { if (Set(ref favoritesFirst, value)) ApplyServerSort(); } }
    public bool IsAuto => library.Current.SelectionMode == SelectionMode.Auto;
    public string SelectedModeDisplay => Resource(IsAuto ? "AutoShort" : "ManualMode");
    public string? ResolvedServerId { get => resolvedServerId; private set { Set(ref resolvedServerId, value); RaiseAuto(); } }
    public string AutoServerDisplay => library.Current.Profiles.Find(p => p.Server.Id == ResolvedServerId) is { } profile ? SafeServerName(profile.Server.Name) : Resource(fullScopeFailed ? "NoHealthyServers" : "AutoNotEvaluated");
    public string AutoLatencyDisplay => library.Current.Profiles.Find(p => p.Server.Id == ResolvedServerId) is { } p && selection.Recent(p, SelectedCore) is { IsHealthy: true } r ? $"{r.Milliseconds} ms" : "—";
    public string TotalServersDisplay => $"{Resource("TotalLabel")}: {Servers.Count}";
    public bool Testing { get => testing; private set { Set(ref testing, value); Raise(nameof(CanTest)); Raise(nameof(CanEdit)); Raise(nameof(HasTestProgress)); Raise(nameof(TestProgressText)); } }
    public bool HasTestProgress => Testing || testTotal > 0;
    public bool CanTest => !Testing && !Busy && Servers.Count > 0;
    public string TestProgressText => $"{Resource(Testing ? "TestingServers" : "TestedServers")}: {testCompleted} / {testTotal} · {Resource("HealthyFilter")}: {testHealthy} · {Resource("FailedFilter")}: {testFailed}";
    public ICommand CopyIpCommand { get; private set; } = null!;
    public ICommand SortServersCommand { get; private set; } = null!;
    public ICommand FilterServersCommand { get; private set; } = null!;
    public ICommand ConnectSelectedCommand { get; private set; } = null!;
    public ICommand TestAllCommand { get; private set; } = null!;
    public ICommand CancelTestsCommand { get; private set; } = null!;
    public ICommand AutoCommand { get; private set; } = null!;
    public ICommand ManualCommand { get; private set; } = null!;
    public ICommand UpdateAllCommand { get; private set; } = null!;
    public BulkCollection<ServerRow> ServerRows { get; } = [];
    public ObservableCollection<SubscriptionRow> SubscriptionRows { get; } = [];
    public ServerRow? SelectedServerRow { get => ServerRows.FirstOrDefault(r => r.Profile == SelectedServer); set { if (value != null) SelectedServer = value.Profile; Raise(); } }
    public SubscriptionRow? SelectedSubscriptionRow { get => SubscriptionRows.FirstOrDefault(r => r.Source == SelectedSubscription); set { if (value != null) SelectedSubscription = value.Source; Raise(); } }
    public void PrepareImport() { ProfileInput = ""; ProfileResult = ""; }
    private void InitializePremium()
    {
        ServerView = CollectionViewSource.GetDefaultView(ServerRows);
        ServerView.Filter = item => item is ServerRow row && ServerBrowser.Matches(row.Profile, row.Result, Search, serverFilter);
        CopyIpCommand = new RelayCommand(_ => { if (HasPublicIp) System.Windows.Clipboard.SetText(PublicIp); });
        SortServersCommand = new RelayCommand(field => {
            serverSort = field?.ToString() switch { "Server.Name" => ServerSort.Name, "Server.Protocol" => ServerSort.Protocol, _ => Enum.TryParse<ServerSort>(field?.ToString(), out var sort) ? sort : ServerSort.Name };
            ApplyServerSort();
        });
        FilterServersCommand = new RelayCommand(filter => { serverFilter = Enum.TryParse<ServerFilter>(filter?.ToString(), out var parsed) ? parsed : ServerFilter.All; ServerView.Refresh(); });
        ConnectSelectedCommand = new AsyncCommand(async () => { if (SelectedServer != null) await ConnectRowAsync(SelectedServer); }, HandleFailure);
        TestAllCommand = new AsyncCommand(() => StartTestsAsync(), HandleFailure);
        CancelTestsCommand = new RelayCommand(_ => { testCancellation?.Cancel(); if (testCancellation == null && Testing) networkCancellation?.Cancel(); });
        AutoCommand = new AsyncCommand(async () => { EnsureIdle(); await library.SetSelectionModeAsync(SelectionMode.Auto); RaiseAuto(); await StartTestsAsync(); }, HandleFailure);
        ManualCommand = new AsyncCommand(async () => { EnsureIdle(); await library.SetSelectionModeAsync(SelectionMode.Manual); RaiseAuto(); }, HandleFailure);
        UpdateAllCommand = new AsyncCommand(async () => {
            EnsureIdle(); Busy = true; networkCancellation = new();
            try { foreach (var sub in library.Current.Subscriptions.ToArray()) await library.UpdateSubscriptionAsync(sub.Id, networkCancellation.Token); RefreshLibrary(); Notice = "Subscriptions updated."; }
            finally { RefreshLibrary(); Busy = false; networkCancellation.Dispose(); networkCancellation = null; }
        }, HandleFailure);
        durationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        durationTimer.Tick += (_, _) => { Raise(nameof(Duration)); if (!Testing) RefreshCachedResults(); Raise(nameof(FastestServers)); BeginMonitoringTick(); };
        durationTimer.Start();
        ApplyServerSort();
    }
    private void ApplyServerSort() { if (ServerView is ListCollectionView list) list.CustomSort = new RowComparer(serverSort, FavoritesFirst); }
    private sealed class RowComparer(ServerSort sort, bool favoritesFirst) : IComparer
    {
        public int Compare(object? a, object? b) => a is ServerRow left && b is ServerRow right ? ServerBrowser.Compare(left.Profile, left.Result, right.Profile, right.Result, sort, favoritesFirst) : 0;
    }
    private void RaiseAuto()
    {
        foreach (var property in new[] { nameof(IsAuto), nameof(SelectedModeDisplay), nameof(AutoServerDisplay), nameof(AutoLatencyDisplay), nameof(ActiveServer), nameof(HasActiveProfile), nameof(CanTest), nameof(TotalServersDisplay), nameof(Latency) }) Raise(property);
    }
    private void RefreshPremiumLibrary()
    {
        var scope = SelectedCore + ":" + string.Join(";", Servers.Select(p => p.Server.Id + ProfileIdentity.Of(p.ShareUri)).Order(StringComparer.Ordinal));
        if (scope != libraryScope)
        {
            libraryScope = scope; fullScopeFailed = false; if (State != ConnectionState.Connected) ResolvedServerId = null;
            testTotal = testCompleted = testHealthy = testFailed = 0; Raise(nameof(TestProgressText)); Raise(nameof(HasTestProgress));
        }
        selection.RetainProfiles(Servers.Select(p => p.Server.Id));
        var old = ServerRows.ToDictionary(r => r.Server.Id);
        var nextRows = new List<ServerRow>();
        {
            foreach (var profile in Servers)
            {
                var row = old.TryGetValue(profile.Server.Id, out var prior) ? prior : new ServerRow(profile);
                var sameConfiguration = prior != null && prior.Core == SelectedCore && prior.Profile.ShareUri == profile.ShareUri;
                if (!sameConfiguration) row.SetCompatibility(library.CoreCompatibilityError(profile, SelectedCore));
                var measured = selection.Recent(profile, SelectedCore);
                if (measured == null && sameConfiguration && prior?.Result?.Health is ServerHealth.Cancelled or ServerHealth.Testing) measured = prior.Result;
                row.Update(profile, SelectedCore, measured);
                row.ConnectCommand = new AsyncCommand(() => ConnectRowAsync(row.Profile), HandleFailure);
                row.SelectCommand = new AsyncCommand(async () => { EnsureIdle(); SelectedServer = row.Profile; await library.SelectAsync(row.Server.Id); await library.SetSelectionModeAsync(SelectionMode.Manual); RefreshLibrary(); }, HandleFailure);
                row.TestCommand = new AsyncCommand(() => StartTestsAsync(row.Profile), HandleFailure);
                row.FavoriteCommand = new AsyncCommand(async () => { await library.ToggleFavoriteAsync(row.Server.Id); RefreshLibrary(); }, HandleFailure);
                row.RemoveCommand = new AsyncCommand(async () => { EnsureIdle(); if (Confirm(Resource("RemoveServerConfirm"))) { await library.RemoveProfileAsync(row.Server.Id); RefreshLibrary(); } }, HandleFailure);
                row.CountryCommand = new RelayCommand(_ => CountryEditRequested?.Invoke(this, row.Server.Id));
                nextRows.Add(row);
            }
        }
        ServerRows.ReplaceAll(nextRows);
        RefreshRowSelection();
        SubscriptionRows.Clear(); foreach (var source in Subscriptions)
        {
            var row = new SubscriptionRow(source);
            row.UpdateCommand = new AsyncCommand(async () => { SelectedSubscription = source; await UpdateSubscriptionByIdAsync(source.Id); }, HandleFailure);
            row.EditCommand = new RelayCommand(_ => { SelectedSubscription = source; SubscriptionEditRequested?.Invoke(this, source.Id); });
            row.RemoveCommand = new AsyncCommand(async () => { EnsureIdle(); if (Confirm(Resource("RemoveSubscriptionConfirm"))) { await library.RemoveSubscriptionAsync(source.Id); RefreshLibrary(); } }, HandleFailure);
            SubscriptionRows.Add(row);
        }
        foreach (var sub in SubscriptionRows) sub.Summary = $"{library.Current.Profiles.Count(p => p.SubscriptionId == sub.Id)} {Resource("Servers")} · " + sub.UsageDisplay;
        if (ResolvedServerId != null && !Servers.Any(p => p.Server.Id == ResolvedServerId && selection.Recent(p, SelectedCore)?.IsHealthy == true)) ResolvedServerId = null;
        Raise(nameof(NeedsSubscription)); Raise(nameof(HasSubscriptions)); Raise(nameof(SelectedServerRow)); Raise(nameof(SelectedSubscriptionRow)); RaiseAuto();
    }
    public event EventHandler<string>? SubscriptionEditRequested;
    public event EventHandler<string>? CountryEditRequested;
    public async Task SaveCountryAsync(string id, string? country) { await library.SetCountryAsync(id, country); RefreshLibrary(); }
    private void RefreshRowSelection()
    {
        foreach (var row in ServerRows) row.SetSelected(row.Server.Id == (State == ConnectionState.Connected ? connectedServer?.Id : IsAuto ? ResolvedServerId : library.Current.ActiveId));
    }
    private void RefreshCachedResults()
    {
        var changed = false;
        foreach (var row in ServerRows) { var result = row.WithCompatibility(selection.Recent(row.Profile, SelectedCore)); if (result == null && row.Result?.Health == ServerHealth.Cancelled) result = row.Result; if (row.Result != result) { row.Update(row.Profile, SelectedCore, result); changed = true; } }
        if (changed) { if (State != ConnectionState.Connected && ResolvedServerId != null && !ServerRows.Any(r => r.Server.Id == ResolvedServerId && r.Result?.IsHealthy == true)) ResolvedServerId = null; ServerView.Refresh(); RaiseAuto(); }
    }
    private async Task ConnectRowAsync(SavedProfile profile)
    {
        if (State == ConnectionState.Connected)
        {
            if (!Confirm(Resource("SwitchServerConfirm"))) return;
            manualDisconnect = true;
            await DisconnectAsync();
        }
        EnsureIdle(); SelectedServer = profile; await library.SetSelectionModeAsync(SelectionMode.Manual);
        CurrentPage = "Dashboard"; connectOperation = ConnectAsync(); await connectOperation;
    }
    private IProgress<ServerTestProgress> CreateProgress() => new Progress<ServerTestProgress>(p => {
        testTotal = p.Total; testCompleted = p.Completed; testHealthy = p.Successful; testFailed = p.Failed;
        var row = ServerRows.FirstOrDefault(r => r.Server.Id == p.Result.ProfileId);
        if (row != null) row.Update(row.Profile, SelectedCore, p.Result);
        Raise(nameof(TestProgressText));
        Raise(nameof(HasTestProgress));
    });
    private async Task<AutoResolution> EvaluateServersAsync(bool force, CancellationToken token)
    {
        var snapshot = library.Current;
        Testing = true; testTotal = snapshot.Profiles.Count; testCompleted = testHealthy = testFailed = 0; Raise(nameof(TestProgressText));
        try { var result = await selection.ResolveForConnectionAsync(snapshot, force, CreateProgress(), token); fullScopeFailed = result.CompleteScope && result.ResolvedServerId == null; return result; }
        finally { Testing = false; }
    }
    private async Task TestAllAsync(SavedProfile? single = null)
    {
        if (Testing || Busy) return;
        fullScopeFailed = false;
        if (State != ConnectionState.Connected) ResolvedServerId = null;
        foreach (var row in ServerRows.Where(r => single == null || r.Server.Id == single.Server.Id)) row.Update(row.Profile, SelectedCore, null);
        Testing = true; testCancellation = new(); testTotal = single == null ? Servers.Count : 1; testCompleted = testHealthy = testFailed = 0; Raise(nameof(TestProgressText));
        try
        {
            var result = await selection.EvaluateAsync(single == null ? library.Current.Profiles.ToArray() : [single], SelectedCore, true, CreateProgress(), testCancellation.Token);
            // Retesting can resolve the next connection, but never relabel or switch the active one.
            if (single == null && State != ConnectionState.Connected) ResolvedServerId = result.ResolvedServerId;
            if (single == null) fullScopeFailed = result.CompleteScope && result.ResolvedServerId == null;
            if (single == null && IsAuto && result.ResolvedServerId == null) Notice = Resource("NoHealthyServers");
        }
        finally
        {
            Testing = false; testCancellation.Dispose(); testCancellation = null;
            foreach (var row in ServerRows.Where(r => r.Result?.Health == ServerHealth.Testing)) row.Update(row.Profile, SelectedCore, new(row.Server.Id, ServerHealth.Cancelled, null, DateTimeOffset.UtcNow, "Cancelled."));
            ServerView.Refresh(); RaiseAuto(); RefreshRowSelection();
        }
    }
    private async Task StartTestsAsync(SavedProfile? single = null)
    {
        if (Testing || Busy) return;
        testOperation = TestAllAsync(single);
        await testOperation;
    }
}

public sealed class BulkCollection<T> : ObservableCollection<T>
{
    public void ReplaceAll(IEnumerable<T> values)
    {
        Items.Clear(); foreach (var value in values) Items.Add(value);
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
    }
}

public sealed class ServerRow(SavedProfile profile) : ObservableObject
{
    private string? compatibilityError;
    private static string Text(string key) => System.Windows.Application.Current.TryFindResource(key) as string ?? key;
    public SavedProfile Profile { get; private set; } = profile;
    public Server Server => Profile.Server;
    public string NameDisplay => Server.Name.Contains("://", StringComparison.Ordinal) ? Text("ImportedServerLabel") : Server.Name;
    public CoreKind Core { get; private set; }
    public ServerTestResult? Result { get; private set; }
    public bool IsSelected { get; private set; }
    public string StatusDisplay => Text("Health" + (Result?.Health ?? ServerHealth.Untested));
    public string CountryDisplay => Server.Country ?? Text("UnknownLocation");
    public ImageSource Flag => CountryFlags.Get(Server.Country);
    public string PingDisplay => Result?.IsHealthy == true ? $"{Result.Milliseconds} ms" : Text(Result?.Health == ServerHealth.Unsupported ? "HealthUnsupportedShort" : "Health" + (Result?.Health ?? ServerHealth.Untested));
    public string LastTestedDisplay => Result is { Health: not ServerHealth.Testing } r && r.TestedAt != DateTimeOffset.MinValue ? $"{Text("LastTestedLabel")}: {r.TestedAt.ToLocalTime():g}" : Text("HealthUntested");
    public string TestExplanation => Result?.Explanation ?? Text("HttpsLatencyHint");
    public string SelectionDisplay => IsSelected ? Text("ActiveLabel") : "—";
    public string ProtocolDisplay => Server.Protocol + " · " + (Core == CoreKind.Xray ? "Xray" : "sing-box");
    public string FavoriteDisplay => Profile.IsFavorite ? "★" : "☆";
    public Brush StatusBrush => System.Windows.Application.Current.TryFindResource(Result?.Health switch
    {
        ServerHealth.Healthy => "ConnectedBrush", ServerHealth.Degraded or ServerHealth.Timeout => "WarningBrush",
        ServerHealth.Failed or ServerHealth.Unsupported => "ErrorBrush", ServerHealth.Testing => "AccentBrush", _ => "SecondaryTextBrush"
    }) as Brush ?? Brushes.Gray;
    public ICommand ConnectCommand { get; set; } = null!;
    public ICommand SelectCommand { get; set; } = null!;
    public ICommand TestCommand { get; set; } = null!;
    public ICommand FavoriteCommand { get; set; } = null!;
    public ICommand RemoveCommand { get; set; } = null!;
    public ICommand CountryCommand { get; set; } = null!;
    public void SetSelected(bool selected) { IsSelected = selected; Raise(nameof(IsSelected)); Raise(nameof(SelectionDisplay)); }
    public void SetCompatibility(string? error) => compatibilityError = error;
    public ServerTestResult? WithCompatibility(ServerTestResult? result) => result ?? (compatibilityError == null ? null : new(Server.Id, ServerHealth.Unsupported, null, DateTimeOffset.MinValue, compatibilityError));
    public void Update(SavedProfile value, CoreKind core, ServerTestResult? result)
    {
        Profile = value; Core = core; Result = WithCompatibility(result);
        foreach (var property in new[] { nameof(Profile), nameof(Server), nameof(NameDisplay), nameof(Flag), nameof(CountryDisplay), nameof(StatusDisplay), nameof(PingDisplay), nameof(LastTestedDisplay), nameof(TestExplanation), nameof(ProtocolDisplay), nameof(FavoriteDisplay), nameof(StatusBrush) }) Raise(property);
    }
}
public sealed class SubscriptionRow(Subscription source)
{
    private static string Text(string key) => System.Windows.Application.Current.TryFindResource(key) as string ?? key;
    public Subscription Source => source;
    public string Id => source.Id;
    public string Name => source.Name;
    public ICommand UpdateCommand { get; set; } = null!;
    public ICommand EditCommand { get; set; } = null!;
    public ICommand RemoveCommand { get; set; } = null!;
    public string Summary { get; set; } = "";
    public string UsageDisplay => $"{Text("RemainingLabel")}: {(source.RemainingBytes is long bytes ? $"{bytes / 1073741824d:0.##} GB ({Text("ProviderLabel")})" : Text("UnavailableLabel"))} · {Text("ExpiresLabel")}: {(source.ExpiresAt is { } expires ? expires.ToLocalTime().ToString("g") : Text("UnavailableLabel"))}";
    public string UpdateDisplay => source.UpdatedAt is { } time ? $"{Text("UpdatedLabel")} {time.ToLocalTime():g}" : Text("NotUpdatedLabel");
}
