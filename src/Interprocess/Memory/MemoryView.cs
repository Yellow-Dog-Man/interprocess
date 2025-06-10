using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Cloudtoid.Interprocess.Memory.Unix;
using Cloudtoid.Interprocess.Memory.Windows;
using SysPath = System.IO.Path;

namespace Cloudtoid.Interprocess;

// This class manages the underlying Memory Mapped File
public sealed class MemoryView : IDisposable
{
    private readonly IMemoryFile file;
    private readonly MemoryMappedViewAccessor view;

    public unsafe MemoryView(MemoryViewOptions options, ILoggerFactory loggerFactory)
    {
        // Check if the path is different from the default temp path and use the Unix one instead.
        // This allows defining a custom map location, even on Windows. Though it's also useful for
        // IPC via Wine/Proton as well since you can point it at /tmp, which resides in memory.

        file =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && SysPath.GetTempPath() == options.Path
            ? new MemoryFileWindows(options)
            : new MemoryFileUnix(options, loggerFactory); // The Unix implementation actually appears compatible with Windows.

        try
            {
                view = file.MappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);

                try
                {
                    Pointer = AcquirePointer();
                }
                catch
                {
                    view.Dispose();
                    throw;
                }
            }
            catch
            {
                file.Dispose();
                throw;
            }
    }

#pragma warning disable CA1720 // This is quite literally... a pointer.
    public unsafe byte* Pointer { get; }
#pragma warning restore CA1720

    public unsafe Span<byte> Data => new(Pointer, (int)view.Capacity);

    public void Dispose()
    {
        view.SafeMemoryMappedViewHandle.ReleasePointer();
        view.Flush();
        view.Dispose();
        file.Dispose();
    }

    private unsafe byte* AcquirePointer()
    {
        byte* ptr = null;
        view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        if (ptr is null)
            throw new InvalidOperationException("Failed to acquire a pointer to the memory mapped file view.");

        return ptr;
    }
}