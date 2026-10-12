using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using AJ.VPN.Application;
using AJ.VPN.Domain;

namespace AJ.VPN.Infrastructure;

public static class ProtectedFiles
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SHAGHAD/v1/local-secret-store");
    public static void CreatePrivateDirectory(string directory)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        // Reject junctions/symlinks through the entire existing path, including ancestors.
        for (var item = new DirectoryInfo(Path.GetFullPath(directory)); item != null; item = item.Parent)
            if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Reparse paths are not accepted for secret storage.");
        Directory.CreateDirectory(directory);
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(true, false);
        var user = WindowsIdentity.GetCurrent().User ?? throw new IOException("Windows user identity is unavailable.");
        security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(directory).SetAccessControl(security);
    }
    public static async Task WriteAsync<T>(string path, T value, CancellationToken token = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        CreatePrivateDirectory(Path.GetDirectoryName(path)!);
        var plain = JsonSerializer.SerializeToUtf8Bytes(value);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
            await File.WriteAllBytesAsync(temp, encrypted, token);
            token.ThrowIfCancellationRequested();
            File.Move(temp, path, true);
        }
        finally { CryptographicOperations.ZeroMemory(plain); if (File.Exists(temp)) File.Delete(temp); }
    }
    public static async Task<T?> ReadAsync<T>(string path, CancellationToken token = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (!File.Exists(path)) return default;
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 || new FileInfo(path).Length > 32_000_000)
            throw new InvalidDataException("The encrypted file is invalid.");
        var encrypted = await File.ReadAllBytesAsync(path, token);
        var plain = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
        try { return JsonSerializer.Deserialize<T>(plain) ?? throw new InvalidDataException("The encrypted file is empty."); }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }
}
public sealed class EncryptedLibraryStore(string directory) : ILibraryStore
{
    private readonly string path = Path.Combine(directory, "secrets", "library.dpapi");
    public async Task<ProfileLibrary> LoadAsync(CancellationToken cancellationToken = default)
    {
        ProtectedFiles.CreatePrivateDirectory(Path.GetDirectoryName(path)!);
        var value = await ProtectedFiles.ReadAsync<ProfileLibrary>(path, cancellationToken) ?? ProfileLibrary.Empty;
        if (value.SchemaVersion != 1 || value.Profiles == null || value.Subscriptions == null || !Enum.IsDefined(value.Core) ||
            value.Profiles.Count > 5000 || value.Subscriptions.Count > 100 || !Enum.IsDefined(value.SelectionMode))
            throw new InvalidDataException("Unsupported or invalid encrypted library. Existing data is preserved.");
        return value;
    }
    public Task SaveAsync(ProfileLibrary library, CancellationToken cancellationToken = default) => ProtectedFiles.WriteAsync(path, library, cancellationToken);
}
