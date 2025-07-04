using System.IO.MemoryMappedFiles;

namespace Cloudtoid.Interprocess.Memory.Unix;

internal sealed class MemoryFileUnix : IMemoryFile
{
    private const string FileExtension = ".qu";
    private const string LockExtension = ".qulock";
    private const int BufferSize = 0x1000;
    private static readonly string Folder = Path.Combine(".cloudtoid", "interprocess", "mmf");
    private readonly string file;
    private readonly string lockFile;
#pragma warning disable CA2213 // Disposable fields should be disposed
    private readonly AtomicFileCounter counter; // This is disposed, the compiler is just silly.
#pragma warning restore CA2213 // Disposable fields should be disposed
    private readonly ILogger<MemoryFileUnix> logger;
    private readonly FileStream? stream;

    internal MemoryFileUnix(MemoryViewOptions options, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<MemoryFileUnix>();
        file = Path.Combine(options.Path, Folder);
        Directory.CreateDirectory(file);
        lockFile = Path.Combine(file, options.MemoryViewName + LockExtension);
        file = Path.Combine(file, options.MemoryViewName + FileExtension);
        counter = new AtomicFileCounter(lockFile, _ => ResetBackingFile(), out _);

        try
        {
            if (AtomicFileCounter.IsMonoUnderLinux)
            {
                MappedFile = MemoryMappedFile.CreateFromFile(
                    file,
                    FileMode.OpenOrCreate,
                    null,
                    BufferSize,
                    MemoryMappedFileAccess.ReadWrite);
            }
            else
            {
                stream = new(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                MappedFile = MemoryMappedFile.CreateFromFile(
                    stream,
                    mapName: null,
                    BufferSize,
                    MemoryMappedFileAccess.ReadWrite,
                    HandleInheritability.None,
                    false);
            }
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
        if (disposing)
        {
            MappedFile.Dispose();
            counter.Dispose();
            stream?.Dispose();
        }
    }

    private void ResetBackingFile()
    {
        // Deletes the backing file if it is not used by any other process
        // if (IsFileInUse())
        //     return;

        if (!PathUtil.TryDeleteFile(file) || !PathUtil.TryDeleteFile(lockFile))
            logger.FailedToDeleteSharedMemoryFile();
    }

    // private bool IsFileInUse() => counter.Count > 0;
}