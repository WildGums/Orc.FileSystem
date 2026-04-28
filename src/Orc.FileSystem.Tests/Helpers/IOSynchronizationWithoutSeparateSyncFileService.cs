namespace Orc.FileSystem.Tests;

using Microsoft.Extensions.Logging;

public class IOSynchronizationWithoutSeparateSyncFileService : IOSynchronizationService
{
    public IOSynchronizationWithoutSeparateSyncFileService(ILogger<IOSynchronizationService> logger, 
        IFileService fileService, IDirectoryService directoryService)
        : base(logger, fileService, directoryService)
    {
    }

    protected override string ResolveObservedFileName(string path)
    {
        return path;
    }
}
