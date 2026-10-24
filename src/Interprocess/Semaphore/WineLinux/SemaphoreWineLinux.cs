namespace Cloudtoid.Interprocess.Semaphore.WineLinux;

internal sealed class SemaphoreWineLinux : IInterprocessSemaphoreWaiter, IInterprocessSemaphoreReleaser
{
    private const string HandleNamePrefix = "/ct.ip.";
    private readonly string name;
    private readonly bool deleteOnDispose;
    private readonly IntPtr handle;

    internal SemaphoreWineLinux(string name, bool deleteOnDispose = false)
    {
        this.name = name = HandleNamePrefix + name;
        this.deleteOnDispose = deleteOnDispose;
        handle = Interop.CreateOrOpenSemaphore(name, 0);
    }

    ~SemaphoreWineLinux() =>
        DisposeCore();

    public void Release() =>
        Interop.Release(handle);

    public bool Wait(int millisecondsTimeout) =>
        Interop.Wait(handle, millisecondsTimeout);

    public void Dispose()
    {
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    private void DisposeCore()
    {
        Interop.Close(handle);

        if (deleteOnDispose)
            Interop.Unlink(name);
    }
}