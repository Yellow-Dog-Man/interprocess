using System.IO.MemoryMappedFiles;

namespace Cloudtoid.Interprocess.Memory.Unix;

internal sealed class MemoryFileUnix : IMemoryFile
{
    public MemoryMappedFile MappedFile { get; }

    private const string FileExtension = ".qu";
    private readonly string file;

    private readonly ILogger<MemoryFileUnix> logger;
    private readonly FileStream stream;

    internal MemoryFileUnix(MemoryViewOptions options, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<MemoryFileUnix>();

        long queueSize = options.GetActualStorageSize();
        string folderPath = options.Path;
        string viewName = options.MemoryViewName;

        Directory.CreateDirectory(folderPath);
        file = Path.Combine(folderPath, $"{viewName}{FileExtension}");

        try
        {
            stream = new(
                file,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.ReadWrite | FileShare.Delete,
                Environment.SystemPageSize,
                options.DestroyOnDispose ? FileOptions.DeleteOnClose : FileOptions.None);

            stream.SetLength(queueSize); // Set length because mono sucks and will throw unless the capacity is just right.
            MappedFile = MemoryMappedFile.CreateFromFile(
                stream,
                mapName: null,
                options.GetActualStorageSize(),
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false);
        }
        catch
        {
            ResetBackingFile();

            throw;
        }
    }

    public void Dispose()
    {
        MappedFile.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ResetBackingFile()
    {
        if (!PathUtil.TryDeleteFile(file))
            logger.FailedToDeleteSharedMemoryFile();
    }

    ~MemoryFileUnix() => Dispose();
}