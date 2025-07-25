using System.Runtime.InteropServices;
using Cloudtoid.Interprocess.Semaphore.Posix;

namespace Cloudtoid.Interprocess.Semaphore.Wine;

internal static partial class Interop
{
    internal static readonly bool ShmBridgeAvailable;

    static Interop()
    {
        try
        {
            SemaphoreClose(IntPtr.Zero);
            ShmBridgeAvailable = true;
        }
        catch
        {
            // Must fall back to Windows semaphores
        }
    }

    private const string Lib = "shmbridge.dll.so";
    private const uint SEMVALUEMAX = 32767;
    private const int OCREAT = 0x040;    // Create the semaphore if it does not exist

    private const int ENOENT = 2;        // The named semaphore does not exist.
    private const int EINTR = 4;         // Semaphore operation was interrupted by a signal.
    private const int EAGAIN = 11;       // Couldn't be acquired (sem_trywait)
    private const int ENOMEM = 12;       // Out of memory
    private const int EACCES = 13;       // Semaphore exists, but the caller does not have permission to open it.
    private const int EEXIST = 17;       // O_CREAT and O_EXCL were specified and the semaphore exists.
    private const int EINVAL = 22;       // Invalid semaphore or operation on a semaphore
    private const int ENFILE = 23;       // Too many semaphores or file descriptors are open on the system.
    private const int EMFILE = 24;       // The process has already reached its limit for semaphores or file descriptors in use.
    private const int ENAMETOOLONG = 36; // The specified semaphore name is too long
    private const int EOVERFLOW = 75;    // The maximum allowable value for a semaphore would be exceeded.
    private const int ETIMEDOUT = 110;   // The call timed out before the semaphore could be locked.

    private static unsafe int Error => Marshal.GetLastWin32Error();

    internal static IntPtr CreateOrOpenSemaphore(string name, uint initialCount)
    {
        var handle = SemaphoreOpen(name, OCREAT, (uint)PosixFilePermissions.ACCESSPERMS, initialCount);
        if (handle != IntPtr.Zero)
            return handle;

        int err = Error;
        switch (err)
        {
            case EINVAL:
                throw new ArgumentException($"One of the arguments passed to sem_open is invalid. Please also ensure {nameof(initialCount)} is less than {SEMVALUEMAX}.");

            case ENAMETOOLONG:
                throw new ArgumentException("The specified semaphore name is too long.", nameof(name));

            case EACCES:
                throw new PosixSemaphoreUnauthorizedAccessException();

            case EEXIST:
                throw new PosixSemaphoreExistsException();

            case EINTR:
                throw new OperationCanceledException();

            case ENFILE:
                throw new PosixSemaphoreException("Too many semaphores or file descriptors are open on the system.");

            case EMFILE:
                throw new PosixSemaphoreException("Too many semaphores or file descriptors are open by this process.");

            case ENOMEM:
                throw new InsufficientMemoryException();

            default:
                throw new PosixSemaphoreException(err);
        }
    }

    internal static void Release(IntPtr handle)
    {
        if (SemaphorePost(handle) == 0)
            return;

        int err = Error;
        switch (err)
        {
            case EINVAL:
                throw new InvalidPosixSemaphoreException();

            case EOVERFLOW:
                throw new SemaphoreFullException();

            default:
                throw new PosixSemaphoreException(err);
        }
    }

    internal static void Close(IntPtr handle)
    {
        if (SemaphoreClose(handle) == 0)
            return;

        int err = Error;
        switch (err)
        {
            case EINVAL:
                throw new InvalidPosixSemaphoreException();

            default:
                throw new PosixSemaphoreException(err);
        }
    }

    internal static void Unlink(string name)
    {
        if (SemaphoreUnlink(name) == 0)
            return;

        int err = Error;
        switch (err)
        {
            case ENAMETOOLONG:
                throw new ArgumentException("The specified semaphore name is too long.", nameof(name));

            case EACCES:
                throw new PosixSemaphoreUnauthorizedAccessException();

            case ENOENT:
                throw new PosixSemaphoreNotExistsException();

            default:
                throw new PosixSemaphoreException(err);
        }
    }

    internal static bool Wait(IntPtr handle, int millisecondsTimeout)
    {
        if (millisecondsTimeout == Timeout.Infinite)
        {
            Wait(handle);
            return true;
        }
        else if (millisecondsTimeout == 0)
        {
            if (SemaphoreTryWait(handle) == 0)
                return true;

            int err = Error;
            switch (err)
            {
                case EINTR:
                case EAGAIN:
                    return false;

                case EINVAL:
                    throw new InvalidPosixSemaphoreException();

                default:
                    throw new PosixSemaphoreException(err);
            }
        }

        var timeout = DateTimeOffset.UtcNow.AddMilliseconds(millisecondsTimeout);
        return Wait(handle, timeout);
    }

    private static void Wait(IntPtr handle)
    {
        if (SemaphoreWait(handle) == 0)
            return;

        int err = Error;
        switch (err)
        {
            case EINTR:
                break;

            case EINVAL:
                throw new InvalidPosixSemaphoreException();

            default:
                throw new PosixSemaphoreException(err);
        }
    }

    private static bool Wait(IntPtr handle, PosixTimespec timeout)
    {
        if (SemaphoreTimedWait(handle, ref timeout) == 0)
            return true;

        int err = Error;
        switch (err)
        {
            case ETIMEDOUT:
            case EINTR:
                return false;

            case EINVAL:
                throw new InvalidPosixSemaphoreException();

            default:
                throw new PosixSemaphoreException(err);
        }
    }

#if NET9_0_OR_GREATER

    [LibraryImport(Lib, EntryPoint = "sem_open", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr SemaphoreOpen(string name, int oflag, uint mode, uint value);

    [LibraryImport(Lib, EntryPoint = "sem_post", SetLastError = true)]
    private static partial int SemaphorePost(IntPtr handle);

    [LibraryImport(Lib, EntryPoint = "sem_wait", SetLastError = true)]
    private static partial int SemaphoreWait(IntPtr handle);

    [LibraryImport(Lib, EntryPoint = "sem_trywait", SetLastError = true)]
    private static partial int SemaphoreTryWait(IntPtr handle);

    [LibraryImport(Lib, EntryPoint = "sem_timedwait", SetLastError = true)]
    private static partial int SemaphoreTimedWait(IntPtr handle, ref PosixTimespec abs_timeout);

    [LibraryImport(Lib, EntryPoint = "sem_unlink", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int SemaphoreUnlink(string name);

    [LibraryImport(Lib, EntryPoint = "sem_close", SetLastError = true)]
    private static partial int SemaphoreClose(IntPtr handle);

#else

    [DllImport(Lib, EntryPoint = "sem_open", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr SemaphoreOpen(string name, int oflag, uint mode, uint value);

    [DllImport(Lib, EntryPoint = "sem_post", SetLastError = true)]
    private static extern int SemaphorePost(IntPtr handle);

    [DllImport(Lib, EntryPoint = "sem_wait", SetLastError = true)]
    private static extern int SemaphoreWait(IntPtr handle);

    [DllImport(Lib, EntryPoint = "sem_trywait", SetLastError = true)]
    private static extern int SemaphoreTryWait(IntPtr handle);

    [DllImport(Lib, EntryPoint = "sem_timedwait", SetLastError = true)]
    private static extern int SemaphoreTimedWait(IntPtr handle, ref PosixTimespec abs_timeout);

    [DllImport(Lib, EntryPoint = "sem_unlink", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern int SemaphoreUnlink(string name);

    [DllImport(Lib, EntryPoint = "sem_close", SetLastError = true)]
    private static extern int SemaphoreClose(IntPtr handle);
#endif
}