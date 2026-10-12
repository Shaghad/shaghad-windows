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
    public Task SetSelectionModeAsync(SelectionMode mode, CancellationToken token = default) => MutateAsync(l =>
        Enum.IsDefined(mode) ? l with { SelectionMode = mode } : throw new InvalidDataException("Invalid selection mode."), token);
    public Task ToggleFavoriteAsync(string id, CancellationToken token = default) => MutateAsync(l => l with
    {
        Profiles = l.Profiles.Select(p => p.Server.Id == id ? p with { IsFavorite = !p.IsFavorite } : p).ToList()
    }, token);
    public Task SetCountryAsync(string id, string? country, CancellationToken token = default)
    {
        var code = country?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(code) && !System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.SpecificCultures)
            .Any(c => new System.Globalization.RegionInfo(c.Name).TwoLetterISORegionName == code))
            throw new InvalidDataException("Choose an ISO country code or leave it blank.");
        return MutateAsync(l => l with { Profiles = l.Profiles.Select(p => p.Server.Id == id
            ? p with { Server = p.Server with { Country = string.IsNullOrEmpty(code) ? null : code } } : p).ToList() }, token);
    }
    public string? CoreCompatibilityError(SavedProfile profile, CoreKind kind) => validator.CoreCompatibilityError(profile.ShareUri, kind);
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
            var existing = l.Profiles.Select(p => ProfileIdentity.Of(p.ShareUri)).ToHashSet(StringComparer.Ordinal);
            var profiles = l.Profiles.Concat(added.Where(a => existing.Add(ProfileIdentity.Of(a.ShareUri)))).ToList();
            if (profiles.Count > 5000) throw new InvalidDataException("The server library is full.");
            return l with { Profiles = profiles, ActiveId = l.ActiveId ?? profiles.FirstOrDefault(p => validator.CoreCompatibilityError(p.ShareUri, l.Core) == null)?.Server.Id };
        }, token);
    }
    public Task SelectAsync(string id, CancellationToken token = default) => MutateAsync(l =>
    {
        var profile = l.Profiles.Find(p => p.Server.Id == id) ?? throw new InvalidDataException("Select an imported server.");
        if (validator.CoreCompatibilityError(profile.ShareUri, l.Core) is string error) throw new CoreCompatibilityException(error);
        return l with { ActiveId = id };
    }, token);
    public Task SetCoreAsync(CoreKind kind, CancellationToken token = default) => MutateAsync(l =>
    {
        if (!Enum.IsDefined(kind)) throw new CoreCompatibilityException("Select a supported core.");
        var active = l.Profiles.Find(p => p.Server.Id == l.ActiveId);
        if (active != null && validator.CoreCompatibilityError(active.ShareUri, kind) is string error) throw new CoreCompatibilityException(error);
        return l with { Core = kind };
    }, token);
    public Task RemoveProfileAsync(string id, CancellationToken token = default) => MutateAsync(l =>
    {
        var profiles = l.Profiles.Where(p => p.Server.Id != id).ToList();
        return l with { Profiles = profiles, ActiveId = l.ActiveId == id ? profiles.FirstOrDefault(p => validator.CoreCompatibilityError(p.ShareUri, l.Core) == null)?.Server.Id : l.ActiveId };
    }, token);
    public Task AddSubscriptionAsync(string name, string url, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new InvalidDataException("Enter a subscription name of up to 100 characters.");
        ValidateSubscriptionUrl(url);
        return MutateAsync(l => l.Subscriptions.Any(s => s.Url == url.Trim()) ? throw new InvalidDataException("This subscription already exists.") : l.Subscriptions.Count >= 100 ? throw new InvalidDataException("The subscription list is full.") :
            l with { Subscriptions = [..l.Subscriptions, new(Guid.NewGuid().ToString("N"), name.Trim(), url.Trim())] }, token);
    }
    public Task EditSubscriptionAsync(string id, string name, string url, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new InvalidDataException("Invalid subscription name.");
        ValidateSubscriptionUrl(url);
        return MutateAsync(l => l.Subscriptions.Any(s => s.Id != id && s.Url == url.Trim()) ? throw new InvalidDataException("This subscription already exists.") : l.Subscriptions.Any(s => s.Id == id)
            ? l with { Subscriptions = l.Subscriptions.Select(s => s.Id == id ? s with { Name = name.Trim(), Url = url.Trim(), UpdatedAt = s.Url == url.Trim() ? s.UpdatedAt : null, RemainingBytes = s.Url == url.Trim() ? s.RemainingBytes : null, ExpiresAt = s.Url == url.Trim() ? s.ExpiresAt : null } : s).ToList() }
            : throw new InvalidDataException("Select a subscription."), token);
    }
    public async Task UpdateSubscriptionAsync(string id, CancellationToken token = default)
    {
        var sub = Current.Subscriptions.Find(s => s.Id == id) ?? throw new InvalidDataException("Select a subscription.");
        var download = downloader is ISubscriptionMetadataDownloader metadata ? await metadata.DownloadWithMetadataAsync(sub.Url, token) : new SubscriptionDownload(await downloader.DownloadAsync(sub.Url, token));
        var input = download.Content;
        var added = await Task.Run(() => Parse(input, id, token), token);
        await MutateAsync(l =>
        {
            if (!l.Subscriptions.Any(s => s.Id == id && s.Url == sub.Url)) throw new InvalidDataException("Subscription changed during download.");
            var old = l.Profiles.Where(p => p.SubscriptionId == id).GroupBy(p => ProfileIdentity.Of(p.ShareUri)).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var retained = l.Profiles.Where(p => p.SubscriptionId != id).ToList();
            var replacement = added.Select(p => old.TryGetValue(ProfileIdentity.Of(p.ShareUri), out var prior) ? p with
                { Server = p.Server with { Id = prior.Server.Id, Country = prior.Server.Country }, IsFavorite = prior.IsFavorite } : p);
            var identities = retained.Select(p => ProfileIdentity.Of(p.ShareUri)).ToHashSet(StringComparer.Ordinal);
            var profiles = retained.Concat(replacement.Where(p => identities.Add(ProfileIdentity.Of(p.ShareUri)))).ToList();
            if (profiles.Count > 5000) throw new InvalidDataException("The server library is full.");
            // Preserve selection when the same wire configuration survives, including renamed remarks.
            var selectedUri = l.Profiles.Find(p => p.Server.Id == l.ActiveId)?.ShareUri;
            var compatible = profiles.Where(p => validator.CoreCompatibilityError(p.ShareUri, l.Core) == null).ToList();
            return l with { Profiles = profiles, ActiveId = compatible.Find(p => selectedUri != null && ProfileIdentity.Of(p.ShareUri) == ProfileIdentity.Of(selectedUri))?.Server.Id ?? compatible.FirstOrDefault()?.Server.Id,
                Subscriptions = l.Subscriptions.Select(s => s.Id == id ? s with { UpdatedAt = DateTimeOffset.UtcNow, RemainingBytes = download.RemainingBytes, ExpiresAt = download.ExpiresAt } : s).ToList() };
        }, token);
    }
    public Task RemoveSubscriptionAsync(string id, CancellationToken token = default) => MutateAsync(l =>
    {
        var profiles = l.Profiles.Where(p => p.SubscriptionId != id).ToList();
        return l with { Subscriptions = l.Subscriptions.Where(s => s.Id != id).ToList(), Profiles = profiles,
            ActiveId = profiles.Any(p => p.Server.Id == l.ActiveId) ? l.ActiveId : profiles.FirstOrDefault(p => validator.CoreCompatibilityError(p.ShareUri, l.Core) == null)?.Server.Id };
    }, token);
    public static Uri ValidateSubscriptionUrl(string url)
    {
        if (url.Length > 8192 || url.Any(char.IsWhiteSpace) || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0)
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
