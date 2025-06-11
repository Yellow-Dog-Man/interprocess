using System.IO.MemoryMappedFiles;

namespace Cloudtoid.Interprocess.Memory.Windows;

internal sealed class MemoryFileWindows : IMemoryFile
{
    private const string MapNamePrefix = "CT_IP_";

    internal MemoryFileWindows(MemoryViewOptions options)
    {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();
#endif
        MappedFile = MemoryMappedFile.CreateOrOpen(
            mapName: MapNamePrefix + options.MemoryViewName,
            options.GetActualStorageSize(),
            MemoryMappedFileAccess.ReadWrite,
            MemoryMappedFileOptions.None,
            HandleInheritability.None);
    }

    public MemoryMappedFile MappedFile { get; }

    public void Dispose() =>
        MappedFile.Dispose();
}