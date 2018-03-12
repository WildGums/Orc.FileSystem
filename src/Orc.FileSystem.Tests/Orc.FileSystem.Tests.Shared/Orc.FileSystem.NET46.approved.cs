﻿[assembly: System.Resources.NeutralResourcesLanguageAttribute("en-US")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("Orc.FileSystem.Tests")]
[assembly: System.Runtime.InteropServices.ComVisibleAttribute(false)]
[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.6", FrameworkDisplayName=".NET Framework 4.6")]


public class static ModuleInitializer
{
    public static void Initialize() { }
}
namespace Orc.FileSystem
{
    
    public class DirectoryService : Orc.FileSystem.IDirectoryService
    {
        public DirectoryService(Orc.FileSystem.IFileService fileService) { }
        public void Copy(string sourcePath, string destinationPath, bool copySubDirs = True, bool overwriteExisting = False) { }
        public string Create(string path) { }
        public void Delete(string path, bool recursive = True) { }
        public bool Exists(string path) { }
        public string[] GetDirectories(string path, string searchPattern = "", System.IO.SearchOption searchOption = 0) { }
        public string[] GetFiles(string path, string searchPattern = "", System.IO.SearchOption searchOption = 0) { }
        public void Move(string sourcePath, string destinationPath) { }
    }
    public class static ExceptionExtensions
    {
        public static int GetHResult(this System.Exception exception) { }
    }
    public class static FileInfoExtensions
    {
        public static System.Threading.Tasks.Task EnsureFilesNotBusyAsync(this System.Collections.Generic.IEnumerable<System.IO.FileInfo> files) { }
    }
    public class FileLocker : System.IDisposable
    {
        public FileLocker(Orc.FileSystem.FileLocker existingLocker) { }
        public void Dispose() { }
        public System.Threading.Tasks.Task LockFilesAsync(params string[] files) { }
        public System.Threading.Tasks.Task LockFilesAsync(System.TimeSpan timeout, params string[] files) { }
    }
    public class static FileLockInfo
    {
        public static string[] GetProcessesLockingFile(string fileName) { }
    }
    public class FileLockScope : Catel.Disposable
    {
        public FileLockScope() { }
        public FileLockScope(bool isReadScope, string syncFile, Orc.FileSystem.IFileService fileService) { }
        public bool NotifyOnRelease { get; set; }
        protected override void DisposeManaged() { }
        public bool Lock() { }
        public void Unlock() { }
        public void WriteDummyContent() { }
    }
    public class FileLockScopeException : System.Exception
    {
        public FileLockScopeException(string message) { }
        public FileLockScopeException(string message, System.Exception innerException) { }
    }
    public class FileService : Orc.FileSystem.IFileService
    {
        public FileService() { }
        public bool CanOpen(string fileName, System.IO.FileMode fileMode, System.IO.FileAccess fileAccess = 3, System.IO.FileShare fileShare = 3) { }
        public void Copy(string sourceFileName, string destinationFileName, bool overwrite = False) { }
        public System.IO.FileStream Create(string fileName) { }
        public void Delete(string fileName) { }
        public bool Exists(string fileName) { }
        public void Move(string sourceFileName, string destinationFileName, bool overwrite = False) { }
        public System.IO.FileStream Open(string fileName, System.IO.FileMode fileMode, System.IO.FileAccess fileAccess = 3, System.IO.FileShare fileShare = 3) { }
    }
    public interface IDirectoryService
    {
        void Copy(string sourcePath, string destinationPath, bool copySubDirs = True, bool overwriteExisting = False);
        string Create(string path);
        void Delete(string path, bool recursive = True);
        bool Exists(string path);
        string[] GetDirectories(string path, string searchPattern = "", System.IO.SearchOption searchOption = 0);
        string[] GetFiles(string path, string searchPattern = "", System.IO.SearchOption searchOption = 0);
        void Move(string sourcePath, string destinationPath);
    }
    public class static IDirectoryServiceExtensions { }
    public interface IFileService
    {
        bool CanOpen(string fileName, System.IO.FileMode fileMode, System.IO.FileAccess fileAccess = 3, System.IO.FileShare fileShare = 3);
        void Copy(string sourceFileName, string destinationFileName, bool overwrite = False);
        System.IO.FileStream Create(string fileName);
        void Delete(string fileName);
        bool Exists(string fileName);
        void Move(string sourceFileName, string destinationFileName, bool overwrite = False);
        System.IO.FileStream Open(string fileName, System.IO.FileMode fileMode, System.IO.FileAccess fileAccess = 3, System.IO.FileShare fileShare = 3);
    }
    public class static IFileServiceExtensions
    {
        public static bool CanOpenRead(this Orc.FileSystem.IFileService fileService, string fileName) { }
        public static bool CanOpenWrite(this Orc.FileSystem.IFileService fileService, string fileName) { }
        public static System.IO.FileStream OpenRead(this Orc.FileSystem.IFileService fileService, string fileName) { }
        public static System.IO.FileStream OpenWrite(this Orc.FileSystem.IFileService fileService, string fileName) { }
        public static byte[] ReadAllBytes(this Orc.FileSystem.IFileService fileService, string fileName) { }
        public static System.Threading.Tasks.Task<byte[]> ReadAllBytesAsync(this Orc.FileSystem.IFileService fileService, string fileName) { }
        public static string[] ReadAllLines(this Orc.FileSystem.IFileService fileService, string fileName) { }
        public static System.Threading.Tasks.Task<string[]> ReadAllLinesAsync(this Orc.FileSystem.IFileService fileService, string fileName) { }
        public static string ReadAllText(this Orc.FileSystem.IFileService fileService, string fileName) { }
        public static System.Threading.Tasks.Task<string> ReadAllTextAsync(this Orc.FileSystem.IFileService fileService, string fileName) { }
        public static void WriteAllBytes(this Orc.FileSystem.IFileService fileService, string fileName, byte[] bytes) { }
        public static System.Threading.Tasks.Task WriteAllBytesAsync(this Orc.FileSystem.IFileService fileService, string fileName, byte[] bytes) { }
        public static void WriteAllLines(this Orc.FileSystem.IFileService fileService, string fileName, string[] lines) { }
        public static void WriteAllLines(this Orc.FileSystem.IFileService fileService, string fileName, System.Collections.Generic.IEnumerable<string> lines) { }
        public static System.Threading.Tasks.Task WriteAllLinesAsync(this Orc.FileSystem.IFileService fileService, string fileName, string[] lines) { }
        public static System.Threading.Tasks.Task WriteAllLinesAsync(this Orc.FileSystem.IFileService fileService, string fileName, System.Collections.Generic.IEnumerable<string> lines) { }
        public static void WriteAllText(this Orc.FileSystem.IFileService fileService, string fileName, string text) { }
        public static System.Threading.Tasks.Task WriteAllTextAsync(this Orc.FileSystem.IFileService fileService, string fileName, string text) { }
    }
    public interface IIOSynchronizationService
    {
        System.TimeSpan DelayAfterWriteOperations { get; set; }
        System.TimeSpan DelayBetweenChecks { get; set; }
        public event System.EventHandler<Orc.FileSystem.PathEventArgs> RefreshRequired;
        System.IDisposable AcquireReadLock(string path);
        System.IDisposable AcquireWriteLock(string path, bool notifyOnRelease = True);
        System.Threading.Tasks.Task ExecuteReadingAsync(string path, System.Func<string, System.Threading.Tasks.Task<bool>> readAsync);
        System.Threading.Tasks.Task ExecuteWritingAsync(string projectLocation, System.Func<string, System.Threading.Tasks.Task<bool>> writeAsync);
        System.Threading.Tasks.Task StartWatchingForChangesAsync(string path);
        System.Threading.Tasks.Task StopWatchingForChangesAsync(string path);
    }
    public class IOSynchronizationException : System.Exception
    {
        public IOSynchronizationException(string message) { }
        public IOSynchronizationException(string message, System.Exception innerException) { }
    }
    public class IOSynchronizationService : Orc.FileSystem.IIOSynchronizationService
    {
        public IOSynchronizationService(Orc.FileSystem.IFileService fileService, Orc.FileSystem.IDirectoryService directoryService) { }
        public System.TimeSpan DelayAfterWriteOperations { get; set; }
        public System.TimeSpan DelayBetweenChecks { get; set; }
        public event System.EventHandler<Orc.FileSystem.PathEventArgs> RefreshRequired;
        public System.IDisposable AcquireReadLock(string path) { }
        public System.IDisposable AcquireWriteLock(string path, bool notifyOnRelease = True) { }
        public System.Threading.Tasks.Task ExecuteReadingAsync(string path, System.Func<string, System.Threading.Tasks.Task<bool>> readAsync) { }
        public System.Threading.Tasks.Task ExecuteWritingAsync(string path, System.Func<string, System.Threading.Tasks.Task<bool>> writeAsync) { }
        protected internal string GetSyncFileByPath(string path) { }
        protected virtual string ResolveObservedFileName(string path) { }
        public System.Threading.Tasks.Task StartWatchingForChangesAsync(string path) { }
        public System.Threading.Tasks.Task StopWatchingForChangesAsync(string path) { }
    }
    public class PathEventArgs : System.EventArgs
    {
        public PathEventArgs(string path) { }
        public string Path { get; }
    }
    public class static StreamExtensions
    {
        public static byte[] ReadAllBytes(this System.IO.Stream stream) { }
        public static System.Threading.Tasks.Task<byte[]> ReadAllBytesAsync(this System.IO.Stream stream) { }
    }
    public class static SystemErrorCodes
    {
        public const uint ERROR_SHARING_VIOLATION = 2147942432u;
    }
}