using AJ.VPN.Application;
using AJ.VPN.Domain;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AJ.VPN.Desktop.ViewModels;

public sealed partial class ShellViewModel
{
    private readonly TrafficRateCalculator trafficCalculator = new();
    private readonly FailoverPolicy failoverPolicy = new();
    private readonly CancellationTokenSource monitoringCancellation = new();
    private bool monitoringBusy, automaticFailover, automaticRecovery, manualDisconnect, suspended;
    private Task? monitoringOperation;
    private Server? desiredServer;
    private long lastTrafficTimestamp;
    private long? lastRecoveryTimestamp;
    private int policyRevision;
    private long lastProbeTimestamp = Stopwatch.GetTimestamp();
    private TrafficRates? traffic;
    public bool AutomaticFailover { get => automaticFailover; set { if (Set(ref automaticFailover, value)) { policyRevision++; failoverPolicy.Reset(); if (!value && monitoringBusy) networkCancellation?.Cancel(); } } }
    public bool AutomaticRecovery { get => automaticRecovery; set { if (Set(ref automaticRecovery, value)) { policyRevision++; if (!value && monitoringBusy) networkCancellation?.Cancel(); } } }
    public string Quality => Resource("Quality" + ConnectionQuality.Classify(State, InternetVerified ? backend.LastProbe : null));
    public System.Windows.Media.ImageSource ActiveFlag => CountryFlags.Get(DisplayServer?.Country);
    public IEnumerable<ServerRow> FastestServers => ServerRows.Where(r => selection.Recent(r.Profile, SelectedCore)?.IsHealthy == true)
        .OrderBy(r => r.Result?.Milliseconds).ThenBy(r => r.Server.Id, StringComparer.Ordinal).Take(3);
    public string DownloadSpeed => FormatRate(traffic?.DownloadPerSecond);
    public string UploadSpeed => FormatRate(traffic?.UploadPerSecond);
    public string TotalDownloaded => traffic == null ? Resource("UnavailableMetric") : FormatBytes(traffic.Downloaded);
    public string TotalUploaded => traffic == null ? Resource("UnavailableMetric") : FormatBytes(traffic.Uploaded);
    private static string FormatBytes(double bytes) => bytes >= 1024 * 1024 ? $"{bytes / (1024 * 1024):0.0} MB" : bytes >= 1024 ? $"{bytes / 1024:0.0} KB" : $"{bytes:0} B";
    private static string FormatRate(double? bytes) => bytes.HasValue ? FormatBytes(bytes.Value) + "/s" : "—";
    private void RaiseMonitoring()
    {
        foreach (var name in new[] { nameof(Quality), nameof(ActiveFlag), nameof(FastestServers), nameof(DownloadSpeed), nameof(UploadSpeed), nameof(TotalDownloaded), nameof(TotalUploaded) }) Raise(name);
    }
    private void BeginMonitoringTick()
    {
        if (monitoringBusy || monitoringCancellation.IsCancellationRequested || suspended || Stopwatch.GetElapsedTime(lastTrafficTimestamp) < TimeSpan.FromSeconds(2)) return;
        lastTrafficTimestamp = Stopwatch.GetTimestamp();
        monitoringOperation = MonitorAsync(false);
    }
    public void SuspendMonitoring() { suspended = true; ipCancellation?.Cancel(); PublicIp = "—"; connection.RecordProbe(new(false, null, "Suspended; verification pending.")); traffic = trafficCalculator.Update(null); RaiseMonitoring(); }
    public void ResumeMonitoring()
    {
        suspended = false;
        if (monitoringBusy || monitoringCancellation.IsCancellationRequested || manualDisconnect || !(AutomaticRecovery || AutomaticFailover)) return;
        if (lastRecoveryTimestamp is long recent && Stopwatch.GetElapsedTime(recent) < TimeSpan.FromSeconds(10)) return;
        lastRecoveryTimestamp = Stopwatch.GetTimestamp();
        monitoringOperation = MonitorAsync(true);
    }
    private async Task MonitorAsync(bool recovery)
    {
        monitoringBusy = true;
        try
        {
            var token = monitoringCancellation.Token;
            var generation = connection.SessionGeneration;
            var revision = policyRevision;
            var current = connectedServer ?? desiredServer;
            if (State == ConnectionState.Connected && backend is ITrafficBackend source)
            {
                var sample = await source.ReadTrafficAsync(token);
                if (generation == connection.SessionGeneration && State == ConnectionState.Connected && !suspended) traffic = trafficCalculator.Update(sample);
                else traffic = trafficCalculator.Update(null);
            }
            else traffic = trafficCalculator.Update(null);
            RaiseMonitoring();
            if (Busy || Testing || manualDisconnect || current == null || suspended) return;
            if (State == ConnectionState.Connected && (recovery || Stopwatch.GetElapsedTime(lastProbeTimestamp) >= TimeSpan.FromSeconds(60)))
            {
                lastProbeTimestamp = Stopwatch.GetTimestamp();
                await ProbeAsync();
                if (generation != connection.SessionGeneration || manualDisconnect || suspended) return;
                failoverPolicy.Record(InternetVerified);
                RaiseMonitoring();
                if (InternetVerified) return;
            }
            else if (State == ConnectionState.Error && recovery) { for (var i = 0; i < 3; i++) failoverPolicy.Record(false); }
            else if (State == ConnectionState.Error && AutomaticFailover && Stopwatch.GetElapsedTime(lastProbeTimestamp) >= TimeSpan.FromSeconds(60))
            { lastProbeTimestamp = Stopwatch.GetTimestamp(); failoverPolicy.Record(false); }
            else if (!recovery) return;
            token.ThrowIfCancellationRequested();
            if (generation != connection.SessionGeneration || revision != policyRevision || manualDisconnect || !(AutomaticFailover || AutomaticRecovery && recovery)) return;
            var candidate = failoverPolicy.Choose(AutomaticFailover, library.Current.SelectionMode, current.Id,
                library.Current.Profiles.Select(p => selection.Recent(p, SelectedCore)).OfType<ServerTestResult>());
            if (candidate == null && !(recovery && AutomaticRecovery && State == ConnectionState.Error && failoverPolicy.CanAttempt)) return;
            // An endpoint change cannot safely transfer the current Windows ownership journal yet.
            if (proxy.IsApplied)
            {
                AutomaticFailover = AutomaticRecovery = false;
                await DisconnectAsync();
                Notice = Resource("RecoveryProxyBlocked"); return;
            }
            candidate ??= current.Id;
            failoverPolicy.BeginAttempt(current.Id, candidate);
            await DisconnectAsync();
            if (monitoringCancellation.IsCancellationRequested || revision != policyRevision || manualDisconnect || suspended) return;
            var profile = library.Current.Profiles.Find(p => p.Server.Id == candidate);
            if (profile == null || library.CoreCompatibilityError(profile, SelectedCore) != null) return;
            await library.SelectAsync(candidate, token);
            // Call the original lifecycle directly with the explicitly ranked cached candidate.
            Busy = true; networkCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            try
            {
                connectedServer = desiredServer = profile.Server;
                await connection.ConnectAsync(profile.Server, networkCancellation.Token);
                connectedAt = Stopwatch.GetTimestamp();
                await ProbeAsync();
                Notice = Resource(InternetVerified ? "RecoveryVerified" : "RecoveryUnverified");
            }
            finally { Busy = false; networkCancellation.Dispose(); networkCancellation = null; RefreshState(); RefreshLibrary(); }
        }
        catch (OperationCanceledException) { }
        catch { Notice = Resource("RecoveryFailed"); }
        finally { monitoringBusy = false; }
    }
}
