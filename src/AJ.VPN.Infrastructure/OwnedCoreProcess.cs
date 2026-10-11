using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AJ.VPN.Infrastructure;

/// <summary>Suspended creation, assignment to a private kill-on-close job, then resume. Never kills by image name.</summary>
public sealed class OwnedCoreProcess : IDisposable
{
    private readonly SafeFileHandle job;
    private readonly StreamReader stdout;
    private readonly StreamReader stderr;
    public Process Process { get; }
    public int DiagnosticChunks { get; private set; }
    public bool GracefulExit { get; private set; }
    public string? LastFailureCategory { get; private set; }
    public static string? ClassifyFailure(string text)
    {
        if (text.Contains("unexpected status", StringComparison.OrdinalIgnoreCase) || text.Contains("unrecognized reply", StringComparison.OrdinalIgnoreCase)) return "Remote transport upgrade rejected";
        if (text.Contains("certificate", StringComparison.OrdinalIgnoreCase) || text.Contains("tls handshake", StringComparison.OrdinalIgnoreCase) || text.Contains("REALITY", StringComparison.OrdinalIgnoreCase)) return "Remote TLS/REALITY handshake failed";
        if (text.Contains("lookup", StringComparison.OrdinalIgnoreCase) || text.Contains("resolve", StringComparison.OrdinalIgnoreCase)) return "Core DNS resolution failed";
        if (text.Contains("dial tcp", StringComparison.OrdinalIgnoreCase) || text.Contains("connectex", StringComparison.OrdinalIgnoreCase)) return "Remote TCP establishment failed";
        if (text.Contains("connection reset", StringComparison.OrdinalIgnoreCase) || text.Contains("EOF", StringComparison.Ordinal)) return "Remote stream closed; exact handshake stage unconfirmed";
        if (text.Contains("ERROR", StringComparison.Ordinal) || text.Contains("error", StringComparison.Ordinal)) return "Core reported an unclassified error";
        return null;
    }
    private OwnedCoreProcess(Process process, SafeFileHandle job, SafeFileHandle output, SafeFileHandle error)
    {
        Process = process; this.job = job;
        stdout = new(new FileStream(output, FileAccess.Read)); stderr = new(new FileStream(error, FileAccess.Read));
        _ = DrainAsync(stdout); _ = DrainAsync(stderr);
    }
    private async Task DrainAsync(StreamReader reader)
    {
        // Discard payloads immediately. Fixed buffers also bound malicious/no-newline log output.
        var buffer = new char[1024];
        var pending = "";
        try
        {
            int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory())) > 0)
            {
                DiagnosticChunks++;
                // Bounded in-memory inspection only; persist fixed categories, never payloads.
                pending += new string(buffer, 0, count);
                var category = ClassifyFailure(pending);
                if (category != null) LastFailureCategory = category;
                if (pending.Length > 4096) pending = pending[^1024..];
            }
        }
        catch (ObjectDisposedException) { } catch (IOException) { }
        finally { Array.Clear(buffer); }
    }
    private static readonly object CreationLock = new();
    public static OwnedCoreProcess Start(string executable, string config, string directory)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        lock (CreationLock)
        {
            var job = CreateJobObjectW(IntPtr.Zero, null);
            if (job.IsInvalid) throw new Win32Exception();
            SafeFileHandle? output = null, error = null, outputWrite = null, errorWrite = null, input = null;
            ProcessInformation pi = default;
            IntPtr attributes = IntPtr.Zero, handles = IntPtr.Zero;
            var attributesInitialized = false;
            try
            {
                var limits = new ExtendedLimits { Basic = new BasicLimits { LimitFlags = 0x2000 } };
                if (!SetInformationJobObject(job, 9, ref limits, (uint)Marshal.SizeOf<ExtendedLimits>())) throw new Win32Exception();
                var security = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>(), Inherit = true };
                if (!CreatePipe(out output, out outputWrite, ref security, 0) || !CreatePipe(out error, out errorWrite, ref security, 0)) throw new Win32Exception();
                if (!SetHandleInformation(output, 1, 0) || !SetHandleInformation(error, 1, 0)) throw new Win32Exception();
                input = CreateFileW("NUL", 0x80000000, 3, ref security, 3, 0, IntPtr.Zero);
                if (input.IsInvalid) throw new Win32Exception();
                var startup = new StartupInfoEx { Startup = new StartupInfo { Size = Marshal.SizeOf<StartupInfoEx>(), Flags = 0x101, ShowWindow = 0,
                    StdInput = input.DangerousGetHandle(), StdOutput = outputWrite.DangerousGetHandle(), StdError = errorWrite.DangerousGetHandle() } };
                nuint attributeSize = 0;
                InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeSize);
                attributes = Marshal.AllocHGlobal((nint)attributeSize);
                if (!InitializeProcThreadAttributeList(attributes, 1, 0, ref attributeSize)) throw new Win32Exception();
                attributesInitialized = true;
                handles = Marshal.AllocHGlobal(3 * IntPtr.Size);
                Marshal.Copy(new[] { startup.Startup.StdInput, startup.Startup.StdOutput, startup.Startup.StdError }, 0, handles, 3);
                if (!UpdateProcThreadAttribute(attributes, 0, (nuint)0x20002, handles, (nuint)(3 * IntPtr.Size), IntPtr.Zero, IntPtr.Zero)) throw new Win32Exception();
                startup.Attributes = attributes;
                // Paths are generated locally, quotes/newlines are forbidden before native command-line construction.
                if (new[] { executable, config, directory }.Any(p => p.IndexOfAny(['"', '\r', '\n']) >= 0)) throw new IOException("Invalid core path.");
                var command = new System.Text.StringBuilder($"\"{executable}\" run -c \"{config}\"");
                if (!CreateProcessW(executable, command, IntPtr.Zero, IntPtr.Zero, true, 0x4 | 0x10 | 0x200 | 0x80000, IntPtr.Zero, directory, ref startup, out pi)) throw new Win32Exception();
                if (!AssignProcessToJobObject(job, pi.Process)) throw new Win32Exception();
                var process = Process.GetProcessById((int)pi.ProcessId);
                if (ResumeThread(pi.Thread) == uint.MaxValue) { process.Dispose(); throw new Win32Exception(); }
                return new(process, job, output, error);
            }
            catch
            {
                if (pi.Process != IntPtr.Zero) TerminateProcess(pi.Process, 1);
                job.Dispose(); output?.Dispose(); error?.Dispose(); throw;
            }
            finally
            {
                outputWrite?.Dispose(); errorWrite?.Dispose(); input?.Dispose();
                if (pi.Thread != IntPtr.Zero) CloseHandle(pi.Thread);
                if (pi.Process != IntPtr.Zero) CloseHandle(pi.Process);
                if (attributes != IntPtr.Zero) { if (attributesInitialized) DeleteProcThreadAttributeList(attributes); Marshal.FreeHGlobal(attributes); }
                if (handles != IntPtr.Zero) Marshal.FreeHGlobal(handles);
            }
        }
    }
    public async Task StopAsync(string helperExecutable, CancellationToken token = default)
    {
        if (Process.HasExited) return;
        // A short-lived helper attaches to the private console; it cannot detach the desktop application's console.
        using var signal = new Process { StartInfo = new(helperExecutable) { UseShellExecute = false, CreateNoWindow = true } };
        signal.StartInfo.ArgumentList.Add("--signal-core"); signal.StartInfo.ArgumentList.Add(Process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        try
        {
            signal.Start();
            using var signalTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try { await signal.WaitForExitAsync(signalTimeout.Token); } catch (OperationCanceledException) { signal.Kill(); }
            using var grace = CancellationTokenSource.CreateLinkedTokenSource(token);
            grace.CancelAfter(TimeSpan.FromSeconds(5));
            await Process.WaitForExitAsync(grace.Token);
            GracefulExit = signal.HasExited && signal.ExitCode == 0;
        }
        catch (Exception ex) when (ex is OperationCanceledException or Win32Exception or InvalidOperationException) { }
        finally
        {
            if (!Process.HasExited) { job.Dispose(); await Process.WaitForExitAsync(CancellationToken.None); }
        }
    }
    public static bool SignalConsoleGroup(int processId)
    {
        FreeConsole();
        if (!AttachConsole((uint)processId)) return false;
        try { return GenerateConsoleCtrlEvent(1, (uint)processId); }
        finally { FreeConsole(); }
    }
    public void Dispose() { job.Dispose(); stdout.Dispose(); stderr.Dispose(); Process.Dispose(); }

    [StructLayout(LayoutKind.Sequential)] private struct SecurityAttributes { public int Length; public IntPtr Descriptor; [MarshalAs(UnmanagedType.Bool)] public bool Inherit; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct StartupInfo
    {
        public int Size; public string? Reserved, Desktop, Title; public uint X, Y, XSize, YSize, XChars, YChars, Fill, Flags;
        public ushort ShowWindow, ReservedSize; public IntPtr ReservedPointer, StdInput, StdOutput, StdError;
    }
    [StructLayout(LayoutKind.Sequential)] private struct ProcessInformation { public IntPtr Process, Thread; public uint ProcessId, ThreadId; }
    [StructLayout(LayoutKind.Sequential)] private struct StartupInfoEx { public StartupInfo Startup; public IntPtr Attributes; }
    [StructLayout(LayoutKind.Sequential)] private struct BasicLimits { public long ProcessTime, JobTime; public uint LimitFlags; public UIntPtr MinWorking, MaxWorking; public uint ActiveLimit; public UIntPtr Affinity; public uint Priority, Scheduling; }
    [StructLayout(LayoutKind.Sequential)] private struct IoCounters { public ulong ReadOps, WriteOps, OtherOps, ReadBytes, WriteBytes, OtherBytes; }
    [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimits { public BasicLimits Basic; public IoCounters Io; public UIntPtr ProcessMemory, JobMemory, PeakProcess, PeakJob; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateJobObjectW(IntPtr security, string? name);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(SafeFileHandle job, int kind, ref ExtendedLimits limits, uint size);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(SafeFileHandle job, IntPtr process);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CreatePipe(out SafeFileHandle read, out SafeFileHandle write, ref SecurityAttributes security, uint size);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetHandleInformation(SafeFileHandle handle, uint mask, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, ref SecurityAttributes security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateProcessW(string app, System.Text.StringBuilder command, IntPtr processSecurity, IntPtr threadSecurity, bool inherit, uint flags, IntPtr environment, string directory, ref StartupInfoEx startup, out ProcessInformation process);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, uint flags, ref nuint size);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, nuint attribute, IntPtr value, nuint size, IntPtr previous, IntPtr returnedSize);
    [DllImport("kernel32.dll")] private static extern void DeleteProcThreadAttributeList(IntPtr list);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(IntPtr thread);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll")] private static extern bool TerminateProcess(IntPtr process, uint code);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AttachConsole(uint processId);
    [DllImport("kernel32.dll")] private static extern bool FreeConsole();
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GenerateConsoleCtrlEvent(uint signal, uint groupId);
}
