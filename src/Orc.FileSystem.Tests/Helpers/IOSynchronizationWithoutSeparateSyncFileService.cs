namespace Orc.FileSystem.Tests
{
    public class IOSynchronizationWithoutSeparateSyncFileService : IOSynchronizationService
    {
        public IOSynchronizationWithoutSeparateSyncFileService(IFileService fileService, IDirectoryService directoryService)
            : base(fileService, directoryService)
        {
        }

        protected override string ResolveObservedFileName(string path)
        {
            return path;
        }
    }
}
