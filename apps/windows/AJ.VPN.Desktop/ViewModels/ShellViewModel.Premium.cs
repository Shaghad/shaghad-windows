using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using AJ.VPN.Domain;
namespace AJ.VPN.Desktop.ViewModels;

public sealed partial class ShellViewModel
{
    private CancellationTokenSource? ipCancellation;
    private string publicIp = "—", search = "";
    private DateTimeOffset? connectedAt;
    private readonly Dictionary<string, string> pings = [];
    private DispatcherTimer? durationTimer;
    public string? EditingSubscriptionId { get; set; }
    public string PublicIp { get => publicIp; private set { Set(ref publicIp, value); Raise(nameof(HasPublicIp)); } }
    public bool HasPublicIp => PublicIp != "—";
    public bool NeedsSubscription => Subscriptions.Count == 0;
    public bool NeedsServer => !HasActiveProfile;
    public bool CanProbe => State == ConnectionState.Connected && !Busy;
    public string ConsumerStatus => State == ConnectionState.Connected
        ? Resource(InternetVerified ? "InternetVerified" : "CoreReadyLabel")
        : Resource(State == ConnectionState.Error ? "ConnectionError" : State.ToString());
    public string Protocol => library.Active?.Server.Protocol.ToUpperInvariant() ?? "—";
    public string Location => library.Active?.Server.Country ?? Resource("UnknownLocation");
    public string ConnectionMode => proxy.IsApplied ? Resource("SystemProxyMode") : Resource("LocalProxyMode");
    public string Duration => connectedAt is { } start && State == ConnectionState.Connected
        ? (DateTimeOffset.UtcNow - start).ToString(@"hh\:mm\:ss") : "—";
    public ICollectionView ServerView { get; private set; } = null!;
    public string Search { get => search; set { if (Set(ref search, value)) ServerView.Refresh(); } }
    public ICommand CopyIpCommand { get; private set; } = null!;
    public ICommand SortServersCommand { get; private set; } = null!;
    public ICommand ConnectSelectedCommand { get; private set; } = null!;
    public ICommand TestAllCommand { get; private set; } = null!;
    public ICommand UpdateAllCommand { get; private set; } = null!;

    public void PrepareImport() { ProfileInput = ""; ProfileResult = ""; }
    private void InitializePremium()
    {
        ServerView = CollectionViewSource.GetDefaultView(ServerRows);
        ServerView.Filter = item => item is ServerRow p && (p.Server.Name.Contains(Search, StringComparison.OrdinalIgnoreCase)
            || p.Server.Protocol.Contains(Search, StringComparison.OrdinalIgnoreCase) || (p.Server.Country?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false));
        CopyIpCommand = new RelayCommand(_ => { if (HasPublicIp) System.Windows.Clipboard.SetText(PublicIp); });
        SortServersCommand = new RelayCommand(field => {
            ServerView.SortDescriptions.Clear();
            ServerView.SortDescriptions.Add(new(field?.ToString() ?? "Server.Name", ListSortDirection.Ascending));
        });
        ConnectSelectedCommand = new AsyncCommand(async () => {
            if (SelectedServer == null) return;
            CurrentPage = "Dashboard"; connectOperation = ConnectAsync(); await connectOperation;
        }, HandleFailure);
        TestAllCommand = new AsyncCommand(TestAllAsync, HandleFailure);
        UpdateAllCommand = new AsyncCommand(async () => {
            EnsureIdle(); Busy = true; networkCancellation = new();
            try { foreach (var sub in library.Current.Subscriptions.ToArray()) await library.UpdateSubscriptionAsync(sub.Id, networkCancellation.Token); RefreshLibrary(); Notice = "Subscriptions updated."; }
            finally { Busy = false; networkCancellation.Dispose(); networkCancellation = null; }
        }, HandleFailure);
        durationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        durationTimer.Tick += (_, _) => Raise(nameof(Duration));
        durationTimer.Start();
    }
    public ObservableCollection<ServerRow> ServerRows { get; } = [];
    public ObservableCollection<SubscriptionRow> SubscriptionRows { get; } = [];
    public ServerRow? SelectedServerRow { get => ServerRows.FirstOrDefault(r => r.Profile == SelectedServer); set { if (value != null) SelectedServer = value.Profile; Raise(); } }
    public SubscriptionRow? SelectedSubscriptionRow { get => SubscriptionRows.FirstOrDefault(r => r.Source == SelectedSubscription); set { if (value != null) SelectedSubscription = value.Source; Raise(); } }
    private void RefreshPremiumLibrary()
    {
        ServerRows.Clear(); foreach (var profile in Servers) ServerRows.Add(new(profile));
        foreach (var p in ServerRows) {
            p.SelectionDisplay = p.Server.Id == library.Current.ActiveId ? Resource("ActiveLabel") : "—";
            p.PingDisplay = pings.GetValueOrDefault(p.Server.Id, "—");
        }
        SubscriptionRows.Clear(); foreach (var source in Subscriptions) SubscriptionRows.Add(new(source));
        foreach (var sub in SubscriptionRows) {
            sub.Summary = $"{library.Current.Profiles.Count(p => p.SubscriptionId == sub.Id)} {Resource("Servers")} · " + sub.UsageDisplay;
        }
        Raise(nameof(NeedsSubscription)); Raise(nameof(SelectedServerRow)); Raise(nameof(SelectedSubscriptionRow)); ServerView.Refresh();
    }
    private async Task TestAllAsync()
    {
        EnsureIdle(); Busy = true; networkCancellation = new();
        var original = library.Current.ActiveId;
        try {
            foreach (var profile in library.Current.Profiles.ToArray()) {
                networkCancellation.Token.ThrowIfCancellationRequested();
                try {
                    await library.SetCoreAsync(SelectedCore, networkCancellation.Token);
                    await library.SelectAsync(profile.Server.Id, networkCancellation.Token);
                    await backend.StartAsync(profile.Server, networkCancellation.Token);
                    var result = await backend.ProbeAsync(networkCancellation.Token);
                    pings[profile.Server.Id] = result.Success ? $"{result.Milliseconds} ms" : "Unavailable";
                }
                catch (OperationCanceledException) { throw; }
                catch { pings[profile.Server.Id] = "Unavailable"; }
                finally { await backend.StopAsync(CancellationToken.None); }
            }
        }
        finally {
            if (original != null) await library.SelectAsync(original);
            Busy = false; networkCancellation.Dispose(); networkCancellation = null; RefreshLibrary(); RefreshState();
        }
    }
}

public sealed class ServerRow(SavedProfile profile)
{
    private static string Text(string key) => System.Windows.Application.Current.TryFindResource(key) as string ?? key;
    public SavedProfile Profile => profile;
    public Server Server => profile.Server;
    public string StatusDisplay => Text(PingDisplay == "—" ? "NotTestedLabel" : PingDisplay == "Unavailable" ? "UnavailableLabel" : "InternetVerified");
    public string CountryDisplay => Server.Country ?? Text("UnavailableLabel");
    public string PingDisplay { get; set; } = "—";
    public string SelectionDisplay { get; set; } = "—";
}
public sealed class SubscriptionRow(Subscription source)
{
    private static string Text(string key) => System.Windows.Application.Current.TryFindResource(key) as string ?? key;
    public Subscription Source => source;
    public string Id => source.Id;
    public string Name => source.Name;
    public string Summary { get; set; } = "";
    public string UsageDisplay => $"{Text("RemainingLabel")}: {(source.RemainingBytes is long bytes ? $"{bytes / 1073741824d:0.##} GB ({Text("ProviderLabel")})" : Text("UnavailableLabel"))} · {Text("ExpiresLabel")}: {(source.ExpiresAt is { } expires ? expires.ToLocalTime().ToString("g") : Text("UnavailableLabel"))}";
    public string UpdateDisplay => source.UpdatedAt is { } time ? $"{Text("UpdatedLabel")} {time.ToLocalTime():g}" : Text("NotUpdatedLabel");
}
