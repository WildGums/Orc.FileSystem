namespace Orc.FileSystem
{
    public interface ISyncFileNameService
    {
        string GetFileName(FileLockScopeContext context);
        string GetFileSearchFilter(FileLockScopeContext context);
        FileLockScopeContext FileLockScopeContextFromFile(string fileName);
    }
}
