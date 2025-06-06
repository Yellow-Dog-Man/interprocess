using System.IO.MemoryMappedFiles;

namespace Cloudtoid.Interprocess.Memory.Unix;

internal sealed class MemoryFileUnix : IMemoryFile
{
#pragma warning disable RCS1213 // Remove unused member declaration
    private const FileAccess FileAccessOption = FileAccess.ReadWrite;
    private const FileShare FileShareOption = FileShare.ReadWrite | FileShare.Delete;
#pragma warning restore RCS1213 // Remove unused member declaration
    private const string Folder = ".cloudtoid/interprocess/mmf";
    private const string FileExtension = ".qu";
    private const int BufferSize = 0x1000;
    private readonly string file;
    private readonly ILogger<MemoryFileUnix> logger;

    internal MemoryFileUnix(QueueOptions options, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<MemoryFileUnix>();
        file = Path.Combine(options.Path, Folder);
        Directory.CreateDirectory(file);
        file = Path.Combine(file, options.QueueName + FileExtension);

#if NET9_0_OR_GREATER
        FileStream stream;

        if (IsFileInUse(file))
        {
            // just open the file

#pragma warning disable CA2000
            stream = new FileStream(
                file,
                FileMode.Open, // just open it
                FileAccessOption,
                FileShareOption,
                BufferSize);
        }
        else
        {
            // override (or create if no longer exist) as it is not being used
            stream = new FileStream(
                file,
                FileMode.Create,
                FileAccessOption,
                FileShareOption,
                BufferSize);
#pragma warning restore CA2000
        }
#endif

        try
        {
#if NET9_0_OR_GREATER
            MappedFile = MemoryMappedFile.CreateFromFile(
                stream,
                mapName: null, // do not set this or it will not work on Linux/Unix/MacOS
                options.GetQueueStorageSize(),
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false);
#else
            MappedFile = MemoryMappedFile.CreateFromFile(
                file,
                FileMode.OpenOrCreate,
                mapName: null,
                BufferSize,
                MemoryMappedFileAccess.ReadWrite);
#endif

        }
        catch
        {
            // do not leave any resources hanging

            try
            {
#if NET9_0_OR_GREATER
                stream.Dispose();
#endif
            }
            catch
            {
                ResetBackingFile();
            }

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
                MappedFile.Dispose();
        }
        finally
        {
            ResetBackingFile();
        }
    }

    private void ResetBackingFile()
    {
        // Deletes the backing file if it is not used by any other process

        if (IsFileInUse(file))
            return;

        if (!PathUtil.TryDeleteFile(file))
            logger.FailedToDeleteSharedMemoryFile();
    }

    private static bool IsFileInUse(string file)
    {
        try
        {
            using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
            {
            }

            return false;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }
}