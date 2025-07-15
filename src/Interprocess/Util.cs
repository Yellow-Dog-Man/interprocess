using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cloudtoid.Interprocess;

internal static class Util
{
    internal static readonly bool IsMonoUnderLinux;
    internal static readonly bool IsWine;
    internal static readonly string MemoryFilePath;

    private const string LINUX_SHM_DIR = "/dev/shm";
    static Util()
    {
        bool isLinux = Environment.OSVersion.Platform == PlatformID.Unix;
        IsMonoUnderLinux = isLinux && Type.GetType("Mono.Runtime") is not null;

        try
        {
            wine_get_version();
            IsWine = true;
        }
        catch
        {
        }

        if (isLinux || IsWine)
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
    private static extern string wine_get_version();
}