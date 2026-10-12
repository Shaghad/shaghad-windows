using AJ.VPN.Application;
using System.Windows.Input;

namespace AJ.VPN.Desktop.ViewModels;

public sealed partial class ShellViewModel
{
    private async Task UpdateSubscriptionByIdAsync(string id)
    {
        EnsureIdle(); Busy = true;
        try
        {
            Notice = Resource("UpdatingSubscription");
            await library.UpdateSubscriptionAsync(id);
            RefreshLibrary();
            var count = library.Current.Profiles.Count(p => p.SubscriptionId == id); Notice = count == 0 ? Resource("NoNewSubscriptionProfiles") : $"{count} {Resource("ImportedCount")}";
        }
        finally { Busy = false; }
    }
    public ImportInspection InspectImport(string text) => SmartImport.Inspect(text, value => library.Parse(value).Count);
    public async Task ImportConfirmedAsync(string text)
    {
        try { ProfileInput = text; await ValidateAsync(); Notice = ProfileResult; CurrentPage = "Servers"; }
        catch (Exception error) { HandleFailure(error); throw; }
    }
    public async Task SaveSubscriptionAsync(bool update)
    {
        try
        {
            EnsureIdle();
            var uri = LibraryService.ValidateSubscriptionUrl(SubscriptionUrl.Trim());
            if (string.IsNullOrWhiteSpace(SubscriptionName)) SubscriptionName = uri.Host;
            var id = EditingSubscriptionId;
            if (id != null) await library.EditSubscriptionAsync(id, SubscriptionName, SubscriptionUrl);
            else
            {
                await library.AddSubscriptionAsync(SubscriptionName, SubscriptionUrl);
                id = library.Current.Subscriptions.Last().Id;
            }
            RefreshLibrary();
            EditingSubscriptionId = id; SelectedSubscription = Subscriptions.First(s => s.Id == id);
            if (update)
            {
                Busy = true;
                try
                {
                    Notice = Resource("UpdatingSubscription");
                    await library.UpdateSubscriptionAsync(id);
                    RefreshLibrary();
                    var count = library.Current.Profiles.Count(p => p.SubscriptionId == id);
                    Notice = count == 0 ? Resource("NoNewSubscriptionProfiles") : $"{count} {Resource("ImportedCount")}";
                }
                finally { Busy = false; }
            }
            else Notice = Resource("SubscriptionSaved");
            CurrentPage = "Subscriptions";
        }
        catch (Exception error) { RefreshLibrary(); HandleFailure(error); throw; }
    }
    public void ReportImportError(Exception error)
    {
        HandleFailure(error);
        if (error is System.IO.InvalidDataException)
        {
            var key = error.Message switch
            {
                var message when message.StartsWith("Only .txt") => "UnsupportedFileHint",
                var message when message.Contains("UTF-8") => "Utf8FileHint",
                var message when message.Contains("2 MB") => "ImportTooLarge",
                var message when message.Contains("already exists") => "DuplicateSubscription",
                var message when message.StartsWith("Subscriptions require") || message.StartsWith("Enter one HTTPS") => "InvalidSubscriptionUrl",
                _ => "UnsupportedImportHint"
            };
            Notice = Resource(key); noticeIsError = true; Raise(nameof(NoticeTitle)); Raise(nameof(NoticeBrush));
        }
    }
    public void DismissNotice() => Notice = "";
    public string NoticeSummary => Notice.Length > 128 ? Notice[..125] + "…" : Notice;
    public string NoticeTitle => Resource(noticeIsError ? "OperationFailed" : "NotificationTitle");
    public System.Windows.Media.Brush NoticeBrush => (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource(noticeIsError ? "ErrorBrush" : State == AJ.VPN.Domain.ConnectionState.Connected && !InternetVerified ? "WarningBrush" : "ConnectedBrush");
    private bool noticeIsError;
    private DateTimeOffset? ipVerifiedAt;
    public string PublicIpDisplay => HasPublicIp ? PublicIp : Resource("ExitIpUnavailable");
    public string IpVerificationDisplay => ipVerifiedAt is { } time ? $"{Resource("LastVerified")}: {time.ToLocalTime():T}" : Resource("ConnectVerifyIp");
    private ICommand? compareIpCommand;
    public ICommand CompareIpCommand => compareIpCommand ??= new AsyncCommand(CompareIpAsync, HandleFailure);
    private async Task CompareIpAsync()
    {
        if (!CanProbe || backend.Port is not int port) return;
        if (!Confirm(Resource("DirectIpConsent"))) return;
        var generation = connection.SessionGeneration;
        var comparison = await IpComparison.MeasureAsync(true, () => AJ.VPN.Infrastructure.PublicExitIp.FetchDirectAsync(), () => AJ.VPN.Infrastructure.PublicExitIp.FetchAsync(port));
        var direct = comparison.Direct;
        var proxied = comparison.Proxy;
        if (generation != connection.SessionGeneration || State != AJ.VPN.Domain.ConnectionState.Connected) return;
        System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow,
            $"{Resource("DirectPublicIp")}: {direct ?? Resource("UnavailableMetric")}\n{Resource("ExitIpLabel")}: {proxied ?? Resource("UnavailableMetric")}\n\n" +
            Resource(comparison.Difference == IpDifference.Unavailable ? "IpDifferenceUnavailable" : comparison.Difference == IpDifference.Changed ? "IpChanged" : "IpSame"),
            "SHAGHAD", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
