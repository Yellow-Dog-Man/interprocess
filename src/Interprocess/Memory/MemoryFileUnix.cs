using System.IO.MemoryMappedFiles;

namespace Cloudtoid.Interprocess.Memory.Unix;

internal sealed class MemoryFileUnix : IMemoryFile
{
    private const string Folder = ".cloudtoid/interprocess/mmf";
    private const string FileExtension = ".qu";
    private const string LockExtension = ".qulock";
    private const int BufferSize = 0x1000;
    private readonly string file;
    private readonly string lockFile;
#pragma warning disable CA2213 // Disposable fields should be disposed
    private readonly AtomicFileCounter counter; // This is disposed, the compiler is just silly.
#pragma warning restore CA2213 // Disposable fields should be disposed
    private readonly ILogger<MemoryFileUnix> logger;

    internal MemoryFileUnix(QueueOptions options, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<MemoryFileUnix>();
        file = Path.Combine(options.Path, Folder);
        Directory.CreateDirectory(file);
        lockFile = Path.Combine(file, options.QueueName + LockExtension);
        file = Path.Combine(file, options.QueueName + FileExtension);
        counter = new AtomicFileCounter(lockFile);

        try
        {
            MappedFile = MemoryMappedFile.CreateFromFile(
                file,
                FileMode.OpenOrCreate,
                mapName: null,
                BufferSize,
                MemoryMappedFileAccess.ReadWrite);
        }
        catch
        {
            ResetBackingFile();

            throw;
        }
    }

    ~MemoryFileUnix() =>
       Dispose(false);

    public MemoryMappedFile MappedFile { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                MappedFile.Dispose();
                counter.Dispose();
            }
        }
        finally
        {
            ResetBackingFile();
        }
    }

    private void ResetBackingFile()
    {
        // Deletes the backing file if it is not used by any other process
        if (IsFileInUse())
            return;

        if (!PathUtil.TryDeleteFile(file) || !PathUtil.TryDeleteFile(lockFile))
            logger.FailedToDeleteSharedMemoryFile();
    }

    private bool IsFileInUse() => counter.Value > 0;
}