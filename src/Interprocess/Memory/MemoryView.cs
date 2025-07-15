using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Cloudtoid.Interprocess.Memory.Unix;
using Cloudtoid.Interprocess.Memory.Windows;

namespace Cloudtoid.Interprocess;

/// <summary>
/// This class manages an underlying Memory Mapped File
/// </summary>
public sealed class MemoryView : IDisposable
{
    private readonly IMemoryFile file;
    private readonly MemoryMappedViewAccessor view;

    /// <summary>
    /// Creates a view over a block of shared memory.
    /// </summary>
    /// <param name="options">Memory view creation options</param>
    /// <param name="loggerFactory">Logger factory</param>
    public unsafe MemoryView(MemoryViewOptions options, ILoggerFactory loggerFactory)
    {
        /*
         * Check if the path is different from the default path and use the Unix one instead.
         * This allows defining a custom map location, even on Windows. Though it's also useful for
         * IPC via Wine/Proton as well since you can point it at /dev/shm, which resides in memory.
         *
         * Honestly at this point the Unix & Windows memory files are interchangeable. We might as
         * well not even have separate ones.
        */

        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (loggerFactory is null)
            throw new ArgumentNullException(nameof(loggerFactory));

        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        bool isDefaultPath = options.Path == Util.MemoryFilePath;

        if (isWindows && isDefaultPath && !Util.IsWine)
            file = new MemoryFileWindows(options);
        else
            file = new MemoryFileUnix(options, loggerFactory); // The Unix implementation actually appears compatible with Windows.

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

    /// <summary>
    /// Pointer to the shared memory block.
    /// </summary>
    public unsafe byte* Pointer { get; }

    /// <summary>
    /// A span over the shared memory block.
    /// </summary>
    public unsafe Span<byte> Data => new(Pointer, (int)view.Capacity);

    /// <summary>
    /// Disposes of this memory view by freeing the underlying viewhandle, accessor and MemoryMappedFile.
    /// </summary>
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