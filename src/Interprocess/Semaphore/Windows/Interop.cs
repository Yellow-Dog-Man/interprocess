using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#if NET9_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace Cloudtoid.Interprocess.Semaphore.Windows;

public static partial class Interop
{
    private const string Lib = "kernel32.dll";

    private const uint WAIT_ABANDONED = 0x00000080;
    private const uint WAIT_OBJECT_0 = 0x00000000;
    private const uint WAIT_TIMEOUT = 0x00000102;
    private const uint WAIT_FAILED = 0xFFFFFFFF;

    internal static SafeWaitHandle CreateOrOpenSemaphore(string name)
    {
        var handle = CreateSemaphoreA(IntPtr.Zero, 0, int.MaxValue, name);
        if (handle.IsInvalid)
            throw new Win32Exception();

        return handle;
    }

    internal static bool Wait(SafeWaitHandle handle, int millisecondsTimeout)
    {
        var result = WaitForSingleObject(handle, millisecondsTimeout);
        return result switch
        {
            WAIT_OBJECT_0 => true,
            WAIT_TIMEOUT => false,
            WAIT_ABANDONED => throw new AbandonedMutexException(),
            WAIT_FAILED => throw new Win32Exception(),
            _ => throw new Exception($"Unknown wait result 0x{result:x8}")
        };
    }

    internal static void Release(SafeWaitHandle handle)
    {
        bool success = ReleaseSemaphore(handle, 1, IntPtr.Zero);
        if (!success)
            throw new Win32Exception();
    }

#if NET9_0_OR_GREATER
    [LibraryImport(Lib, EntryPoint = "CreateSemaphoreA", SetLastError = true, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
    private static partial SafeWaitHandle CreateSemaphoreA(IntPtr lpSemaphoreAttributes, int lInitialCount, int lMaximumCount, string lpName);

    [LibraryImport(Lib, EntryPoint = "WaitForSingleObject", SetLastError = true)]
    private static partial uint WaitForSingleObject(SafeWaitHandle hHandle, int dwMilliseconds);

    [LibraryImport(Lib, EntryPoint = "ReleaseSemaphore", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ReleaseSemaphore(SafeWaitHandle hSemaphore, int lReleaseCount, IntPtr lpPreviousCount);
#else
    [DllImport(Lib, EntryPoint = "CreateSemaphoreA", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern SafeWaitHandle CreateSemaphoreA(IntPtr lpSemaphoreAttributes, int lInitialCount, int lMaximumCount, string lpName);

    [DllImport(Lib, EntryPoint = "WaitForSingleObject", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeWaitHandle hHandle, int dwMilliseconds);

    [DllImport(Lib, EntryPoint = "ReleaseSemaphore", SetLastError = true)]
    private static extern bool ReleaseSemaphore(SafeWaitHandle hSemaphore, int lReleaseCount, IntPtr lpPreviousCount);
#endif
}