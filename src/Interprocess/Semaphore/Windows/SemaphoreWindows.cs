using Microsoft.Win32.SafeHandles;

namespace Cloudtoid.Interprocess.Semaphore.Windows;

// just a wrapper over the Windows named semaphore
// ideally, .NET's own semaphore implementation would be used however on IL2CPP support for named semaphores are missing.
internal sealed class SemaphoreWindows : IInterprocessSemaphoreWaiter, IInterprocessSemaphoreReleaser
{
    private const string HandleNamePrefix = @"Global\CT.IP.";
    private readonly SafeWaitHandle handle;

    internal SemaphoreWindows(string name) => handle = Interop.CreateOrOpenSemaphore(HandleNamePrefix + name);

    public void Dispose() => handle.Dispose();

    public void Release() => Interop.Release(handle);

    public bool Wait(int millisecondsTimeout) => Interop.Wait(handle, millisecondsTimeout);
}