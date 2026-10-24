using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cloudtoid.Interprocess;

internal static class Util
{
    internal static readonly bool IsMonoUnderLinux;
    internal static readonly bool IsWineUnderLinux;
    internal static readonly bool IsWineUnderMacOS;
    internal static bool IsWine => IsWineUnderLinux || IsWineUnderMacOS;
    internal static readonly string MemoryFilePath;

    private const string LINUX_SHM_DIR = "/dev/shm";
    static Util()
    {
        bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        IsMonoUnderLinux = isLinux && Type.GetType("Mono.Runtime") is not null;

        try
        {
            // I was scared off of "out string" or "ref string".
            // At least this behavior is well-known instead of magic
            // compiler shenanigans.
            IntPtr sysname_ptr = IntPtr.Zero;
            IntPtr release_ptr = IntPtr.Zero;

            wine_get_host_version(ref sysname_ptr, ref release_ptr);

            if (sysname_ptr != IntPtr.Zero) {
                // PtrToStringAnsi copies the string from native-land
                // to managed-land.
                string sysname = Marshal.PtrToStringAnsi(sysname_ptr);

                if (sysname == "Linux") IsWineUnderLinux = true;
                if (sysname == "Darwin") IsWineUnderMacOS = true;
            }
        }
        catch
        {
        }

        if (isLinux || IsWineUnderLinux)
            MemoryFilePath = Path.GetFullPath(Path.Combine(LINUX_SHM_DIR, ".cloudtoid", "interprocess", "mmf"));
        else
            MemoryFilePath = Path.GetTempPath();
    }

    internal static void Ensure64Bit()
    {
        if (Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
            return;

        throw new NotSupportedException(
            $"{Assembly.GetExecutingAssembly().GetName().Name} only supports 64-bit processor architectures.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfCancellationRequested(
        this CancellationTokenSource source,
        CancellationToken token = default)
    {
        // NOTE: The source could have been disposed. We can still access the IsCancellationRequested
        // property BUT we cannot access its Token property. Do NOT change this code.
        if (source.IsCancellationRequested)
            throw new OperationCanceledException();

        token.ThrowIfCancellationRequested();
    }

    [DllImport("ntdll.dll", CharSet = CharSet.Ansi)]
    private static extern void wine_get_host_version(ref IntPtr sysname, ref IntPtr release);
}