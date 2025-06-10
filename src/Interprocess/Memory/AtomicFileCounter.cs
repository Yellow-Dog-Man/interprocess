using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace Cloudtoid.Interprocess.Memory.Unix;

/// <summary>
/// Uses interlocked operations on a memory-mapped file to count how many references to it exist.
/// <para>
/// Each instantiation of this class on the same file path will increment it's count by one,<br />
/// and each disposal will decrease the count by one.
/// </para>
/// </summary>
public sealed class AtomicFileCounter : IDisposable
{
    private readonly string path;
    private readonly MemoryMappedFile file;
    private readonly MemoryMappedViewAccessor accessor;
    private readonly unsafe byte* ptr;
    private readonly Action<string>? cb;
    private readonly Mutex locker;
    private int disposed;

    /// <summary>
    /// Creates a new atomic file counter at the specified path.
    /// </summary>
    /// <param name="path">The path to the file counter.</param>
    /// <param name="callback">Callback to invoke when the atomic file counter reaches zero.</param>
    /// <param name="count">The count on the file at the time of instantiation.</param>
#pragma warning disable CA1021
    public unsafe AtomicFileCounter(string path, Action<string>? callback, out int count)
#pragma warning restore CA1021
    {
        locker = new(false, path.Replace('/', '_').Replace('\\', '_'));
        this.path = path;
        cb = callback;
        if (locker.WaitOne())
        {
            file = MemoryMappedFile.CreateFromFile(
                this.path,
                FileMode.OpenOrCreate,
                null,
                4,
                MemoryMappedFileAccess.ReadWrite);

            accessor = file.CreateViewAccessor();
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            count = Interlocked.Increment(ref FileValue);
        }
        else
        {
            throw new Exception("Could not take mutex to atomic file, something is very wrong!!");
        }

        locker.ReleaseMutex();
    }

    /// <summary>
    /// How many references to this file currently exist.
    /// </summary>
    private unsafe ref int FileValue => ref Unsafe.As<byte, int>(ref Unsafe.AsRef<byte>(ptr));

    /// <summary>
    /// Decrements the atomic counter by one and disposes of the memory map.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) == 1)
            return;

        if (Interlocked.Decrement(ref FileValue) == 0 && locker.WaitOne())
        {
            try
            {
                cb?.Invoke(path);
            }
            finally
            {
                locker.ReleaseMutex();
            }
        }

        GC.SuppressFinalize(this);
        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        locker.Dispose();
        accessor.Dispose();
        file.Dispose();
    }
}