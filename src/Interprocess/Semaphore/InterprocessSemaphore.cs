using System.Runtime.InteropServices;
using Cloudtoid.Interprocess.Semaphore.Linux;
using Cloudtoid.Interprocess.Semaphore.MacOS;
using Cloudtoid.Interprocess.Semaphore.Windows;
using Cloudtoid.Interprocess.Semaphore.Wine;

namespace Cloudtoid.Interprocess;

/// <summary>
/// This class opens or creates platform agnostic named semaphore. Named
/// semaphores are synchronization constructs accessible across processes.
/// </summary>
internal static class InterprocessSemaphore
{
    internal static IInterprocessSemaphoreWaiter CreateWaiter(string name)
    {
        if (UseWine)
            return new SemaphoreWine(name);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new SemaphoreWindows(name);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new SemaphoreMacOS(name);

        return new SemaphoreLinux(name);
    }

    internal static IInterprocessSemaphoreReleaser CreateReleaser(string name)
    {
        if (UseWine)
            return new SemaphoreWine(name);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new SemaphoreWindows(name);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new SemaphoreMacOS(name);

        return new SemaphoreLinux(name);
    }

    private static readonly bool UseWine = Util.IsWine && Semaphore.Wine.Interop.ShmBridgeAvailable;
}