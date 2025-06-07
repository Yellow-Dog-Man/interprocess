using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace Cloudtoid.Interprocess.Memory.Unix;

// Uses interlocked operations on a memory-mapped file to count how many references to it exist.
internal sealed class AtomicFileCounter : IDisposable
{
    private readonly MemoryMappedFile file;
    private readonly MemoryMappedViewAccessor accessor;
    private readonly unsafe byte* ptr;
    private int disposed;
    private int lastValue;

    public unsafe AtomicFileCounter(string path)
    {
        file = MemoryMappedFile.CreateFromFile(
            path,
            FileMode.OpenOrCreate,
            null,
            4,
            MemoryMappedFileAccess.ReadWrite);

        accessor = file.CreateViewAccessor();
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        Interlocked.Increment(ref FileValue);
        // Console.WriteLine(Interlocked.Increment(ref FileValue));
    }

    public int Value => Interlocked.CompareExchange(ref disposed, 1, 1) == 1 ? lastValue : FileValue;
    private unsafe ref int FileValue => ref Unsafe.As<byte, int>(ref Unsafe.AsRef<byte>(ptr));

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) == 1)
            return;

        lastValue = Interlocked.Decrement(ref FileValue);

        GC.SuppressFinalize(this);
        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        accessor.Dispose();
        file.Dispose();
    }
}