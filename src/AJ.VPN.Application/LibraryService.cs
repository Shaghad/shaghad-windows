using System.Text;
using AJ.VPN.Domain;

namespace AJ.VPN.Application;

/// <summary>Publish a new snapshot only after the encrypted atomic save succeeds.</summary>
public sealed class LibraryService(ILibraryStore store, IProfileValidator validator, ISubscriptionDownloader downloader)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool loaded;
    public ProfileLibrary Current { get; private set; } = ProfileLibrary.Empty;
    public async Task InitializeAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token);
        try { Current = await store.LoadAsync(token); loaded = true; }
        finally { gate.Release(); }
    }
    public SavedProfile? Active => Current.Profiles.Find(p => p.Server.Id == Current.ActiveId);
    public IReadOnlyList<SavedProfile> Parse(string input, string? subscriptionId = null, CancellationToken token = default)
    {
        if (input.Length > 2_000_000) throw new InvalidDataException("Profile input exceeds the size limit.");
        input = input.Trim();
        if (!input.Contains("://", StringComparison.Ordinal))
        {
            try { input = Encoding.UTF8.GetString(Convert.FromBase64String(input.Replace('-', '+').Replace('_', '/').PadRight((input.Length + 3) / 4 * 4, '='))); }
            catch { throw new InvalidDataException("The subscription is not a supported share-link list."); }
        }
        var lines = input.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length is 0 or > 2000) throw new InvalidDataException("Import requires between 1 and 2000 profiles.");
        var profiles = new List<SavedProfile>();
        foreach (var line in lines.Distinct(StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            var result = validator.Validate(line);
            if (!result.IsValid || result.Server == null) throw new InvalidDataException("Import rejected: a profile is invalid or unsupported. No servers were changed.");
            profiles.Add(new(result.Server, line, subscriptionId));
        }
        return profiles;
    }
    public async Task ImportAsync(string input, CancellationToken token = default)
    {
        var added = await Task.Run(() => Parse(input, token: token), token);
        await MutateAsync(l =>
        {
            var profiles = l.Profiles.Concat(added.Where(a => !l.Profiles.Any(p => p.ShareUri == a.ShareUri))).ToList();
            if (profiles.Count > 5000) throw new InvalidDataException("The server library is full.");
            return l with { Profiles = profiles, ActiveId = l.ActiveId ?? profiles[0].Server.Id };
        }, token);
    }
    public Task SelectAsync(string id, CancellationToken token = default) => MutateAsync(l =>
        l.Profiles.Any(p => p.Server.Id == id) ? l with { ActiveId = id } : throw new InvalidDataException("Select an imported server."), token);
    public Task SetCoreAsync(CoreKind kind, CancellationToken token = default) => MutateAsync(l => l with { Core = kind }, token);
    public Task RemoveProfileAsync(string id, CancellationToken token = default) => MutateAsync(l =>
    {
        var profiles = l.Profiles.Where(p => p.Server.Id != id).ToList();
        return l with { Profiles = profiles, ActiveId = l.ActiveId == id ? profiles.FirstOrDefault()?.Server.Id : l.ActiveId };
    }, token);
    public Task AddSubscriptionAsync(string name, string url, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new InvalidDataException("Enter a subscription name of up to 100 characters.");
        ValidateSubscriptionUrl(url);
        return MutateAsync(l => l.Subscriptions.Count >= 100 ? throw new InvalidDataException("The subscription list is full.") :
            l with { Subscriptions = [..l.Subscriptions, new(Guid.NewGuid().ToString("N"), name.Trim(), url.Trim())] }, token);
    }
    public Task EditSubscriptionAsync(string id, string name, string url, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new InvalidDataException("Invalid subscription name.");
        ValidateSubscriptionUrl(url);
        return MutateAsync(l => l.Subscriptions.Any(s => s.Id == id)
            ? l with { Subscriptions = l.Subscriptions.Select(s => s.Id == id ? s with { Name = name.Trim(), Url = url.Trim() } : s).ToList() }
            : throw new InvalidDataException("Select a subscription."), token);
    }
    public async Task UpdateSubscriptionAsync(string id, CancellationToken token = default)
    {
        var sub = Current.Subscriptions.Find(s => s.Id == id) ?? throw new InvalidDataException("Select a subscription.");
        var input = await downloader.DownloadAsync(sub.Url, token);
        var added = await Task.Run(() => Parse(input, id, token), token);
        await MutateAsync(l =>
        {
            if (!l.Subscriptions.Any(s => s.Id == id && s.Url == sub.Url)) throw new InvalidDataException("Subscription changed during download.");
            var profiles = l.Profiles.Where(p => p.SubscriptionId != id).Concat(added).ToList();
            if (profiles.Count > 5000) throw new InvalidDataException("The server library is full.");
            // Preserve selection when the same share link survives an update.
            var selectedUri = l.Profiles.Find(p => p.Server.Id == l.ActiveId)?.ShareUri;
            return l with { Profiles = profiles, ActiveId = profiles.Find(p => p.ShareUri == selectedUri)?.Server.Id ?? profiles.FirstOrDefault()?.Server.Id,
                Subscriptions = l.Subscriptions.Select(s => s.Id == id ? s with { UpdatedAt = DateTimeOffset.UtcNow } : s).ToList() };
        }, token);
    }
    public Task RemoveSubscriptionAsync(string id, CancellationToken token = default) => MutateAsync(l =>
    {
        var profiles = l.Profiles.Where(p => p.SubscriptionId != id).ToList();
        return l with { Subscriptions = l.Subscriptions.Where(s => s.Id != id).ToList(), Profiles = profiles,
            ActiveId = profiles.Any(p => p.Server.Id == l.ActiveId) ? l.ActiveId : profiles.FirstOrDefault()?.Server.Id };
    }, token);
    public static Uri ValidateSubscriptionUrl(string url)
    {
        if (url.Length > 8192 || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0)
            throw new InvalidDataException("Subscriptions require an HTTPS URL without embedded user credentials or a fragment.");
        return uri;
    }
    private async Task MutateAsync(Func<ProfileLibrary, ProfileLibrary> update, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            if (!loaded) throw new InvalidOperationException("The encrypted library could not be loaded; changes are disabled.");
            var next = update(Current);
            await store.SaveAsync(next, token);
            Current = next;
        }
        finally { gate.Release(); }
    }
}

