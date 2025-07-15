using static Cloudtoid.Contract;

namespace Cloudtoid.Interprocess;

/// <summary> The options to create a raw memory view. </summary>
public class MemoryViewOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryViewOptions"/> class.
    /// </summary>
    /// <param name="memoryViewName">The unique name of the memory view.</param>
    /// <param name="capacity">The maximum capacity of the memory view in bytes. This should be at least 16 bytes long and in the multiples of 8</param>
    public MemoryViewOptions(string memoryViewName, long capacity)
        : this(memoryViewName, Util.MemoryFilePath, capacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryViewOptions"/> class.
    /// </summary>
    /// <param name="memoryViewName">The unique name of the memory view.</param>
    /// <param name="capacity">The maximum capacity of the memory view in bytes. This should be at least 16 bytes long and in the multiples of 8</param>
    /// <param name="destroyOnDispose">Whether to destroy the backing file when this memory map is disposed.<br />NOTE: Only deletes the backing file if a file path is specified!</param>
    public MemoryViewOptions(string memoryViewName, long capacity, bool destroyOnDispose)
        : this(memoryViewName, Util.MemoryFilePath, capacity, destroyOnDispose)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueOptions"/> class.
    /// </summary>
    /// <param name="memoryViewName">The unique name of the queue.</param>
    /// <param name="path">The path to the directory/folder in which the memory mapped and other files are stored in</param>
    /// <param name="capacity">The maximum capacity of the queue in bytes. This should be at least 16 bytes long and in the multiples of 8</param>
    public unsafe MemoryViewOptions(string memoryViewName, string path, long capacity)
    {
        MemoryViewName = CheckNonEmpty(memoryViewName, nameof(memoryViewName));
        Path = CheckValue(path, nameof(path));

        Capacity = CheckGreaterThan(capacity, 16, nameof(capacity));
        CheckParam(
            (capacity % 8) == 0,
            nameof(memoryViewName),
            "messageCapacityInBytes should be a multiple of 8 (8 bytes = 64 bits).");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueOptions"/> class.
    /// </summary>
    /// <param name="memoryViewName">The unique name of the queue.</param>
    /// <param name="path">The path to the directory/folder in which the memory mapped and other files are stored in</param>
    /// <param name="capacity">The maximum capacity of the queue in bytes. This should be at least 16 bytes long and in the multiples of 8</param>
    /// <param name="destroyOnDispose">Whether to destroy the backing file when this memory map is disposed.<br />NOTE: Only deletes the backing file if a file path is specified!</param>
    public unsafe MemoryViewOptions(
        string memoryViewName,
        string path,
        long capacity,
        bool destroyOnDispose) : this(memoryViewName, path, capacity) => DestroyOnDispose = destroyOnDispose;

    /// <summary>
    /// Gets the unique name of the memory view.
    /// </summary>
    public string MemoryViewName { get; }

    /// <summary>
    /// Gets the path to the directory/folder in which the memory mapped and other files are stored in.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the size of the memory view in bytes.
    /// </summary>
    public long Capacity { get; }

    /// <summary>
    /// Gets whether to destroy the backing file once this process has disposed of the memory view.<br />
    /// NOTE: Only deletes the backing file if a file path is specified!
    /// </summary>
    public bool DestroyOnDispose { get; }

    /// <summary>
    /// Gets the full size of this memory view. For raw memory view this is the same as Capacity.
    /// </summary>
    public virtual long GetActualStorageSize() => Capacity;
}