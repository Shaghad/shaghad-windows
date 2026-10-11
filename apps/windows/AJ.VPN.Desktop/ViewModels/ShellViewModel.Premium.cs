using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using AJ.VPN.Domain;
namespace AJ.VPN.Desktop.ViewModels;

public sealed partial class ShellViewModel
{
    private string publicIp = "—", search = "";
    private DateTimeOffset? connectedAt;
    private readonly Dictionary<string, string> pings = [];
    private DispatcherTimer? durationTimer;
    public string? EditingSubscriptionId { get; set; }
    public string PublicIp { get => publicIp; private set { Set(ref publicIp, value); Raise(nameof(HasPublicIp)); } }
    public bool HasPublicIp => PublicIp != "—";
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

    private void InitializePremium()
    {
        ServerView = CollectionViewSource.GetDefaultView(Servers);
        ServerView.Filter = item => item is SavedProfile p && (p.Server.Name.Contains(Search, StringComparison.OrdinalIgnoreCase)
            || p.Server.Protocol.Contains(Search, StringComparison.OrdinalIgnoreCase) || (p.Server.Country?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false));
        CopyIpCommand = new RelayCommand(_ => { if (HasPublicIp) System.Windows.Clipboard.SetText(PublicIp); });
        SortServersCommand = new RelayCommand(_ => {
            ServerView.SortDescriptions.Clear();
            ServerView.SortDescriptions.Add(new("Server.Name", ListSortDirection.Ascending));
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
    private void RefreshPremiumLibrary()
    {
        foreach (var p in Servers) {
            p.SelectionDisplay = p.Server.Id == library.Current.ActiveId ? Resource("ActiveLabel") : "—";
            p.PingDisplay = pings.GetValueOrDefault(p.Server.Id, "—");
        }
        foreach (var sub in library.Current.Subscriptions) {
            sub.Summary = $"{library.Current.Profiles.Count(p => p.SubscriptionId == sub.Id)} servers · Traffic / expiration: unavailable";
        }
        ServerView.Refresh();
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
