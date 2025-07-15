namespace Cloudtoid.Interprocess;

/// <summary> The options to create a queue. </summary>
public sealed class QueueOptions : MemoryViewOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueueOptions"/> class.
    /// </summary>
    /// <param name="queueName">The unique name of the queue.</param>
    /// <param name="capacity">The maximum capacity of the queue in bytes. This should be at least 16 bytes long and in the multiples of 8</param>
    public QueueOptions(string queueName, long capacity)
        : base(queueName, capacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueOptions"/> class.
    /// </summary>
    /// <param name="queueName">The unique name of the queue.</param>
    /// <param name="capacity">The maximum capacity of the queue in bytes. This should be at least 16 bytes long and in the multiples of 8</param>
    /// <param name="deleteOnDispose">Whether to destroy the backing file when this memory map is disposed.<br />NOTE: Only deletes the backing file if a file path is specified!</param>
    public QueueOptions(string queueName, long capacity, bool deleteOnDispose)
        : base(queueName, capacity, deleteOnDispose)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueOptions"/> class.
    /// </summary>
    /// <param name="queueName">The unique name of the queue.</param>
    /// <param name="path">The path to the directory/folder in which the memory mapped and other files are stored in</param>
    /// <param name="capacity">The maximum capacity of the queue in bytes. This should be at least 16 bytes long and in the multiples of 8</param>
    public unsafe QueueOptions(string queueName, string path, long capacity)
        : base(queueName, path, capacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueOptions"/> class.
    /// </summary>
    /// <param name="queueName">The unique name of the queue.</param>
    /// <param name="path">The path to the directory/folder in which the memory mapped and other files are stored in</param>
    /// <param name="capacity">The maximum capacity of the queue in bytes. This should be at least 16 bytes long and in the multiples of 8</param>
    /// <param name="deleteOnDispose">Whether to destroy the backing file when this memory map is disposed.<br />NOTE: Only deletes the backing file if a file path is specified!</param>
    public unsafe QueueOptions(string queueName, string path, long capacity, bool deleteOnDispose)
        : base(queueName, path, capacity, deleteOnDispose)
    {
    }

    /// <summary>
    /// Gets the full size of the queue that includes both the header and message sections
    /// </summary>
    public override unsafe long GetActualStorageSize() => sizeof(QueueHeader) + base.GetActualStorageSize();
}