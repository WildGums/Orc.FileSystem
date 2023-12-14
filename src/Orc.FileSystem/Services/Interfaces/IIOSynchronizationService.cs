namespace Orc.FileSystem;

using System;
using System.Threading.Tasks;

public interface IIOSynchronizationService
{
    TimeSpan DelayBetweenChecks { get; set; }
    TimeSpan DelayAfterWriteOperations { get; set; }

    event EventHandler<PathEventArgs>? RefreshRequired;

    IDisposable AcquireReadLock(string path);
    IDisposable AcquireWriteLock(string path, bool notifyOnRelease = true);

    Task StartWatchingForChangesAsync(string path);
    Task StopWatchingForChangesAsync(string path);

    Task ExecuteReadingAsync(string projectLocation, Func<string, Task<bool>> readAsync);
    Task ExecuteWritingAsync(string projectLocation, Func<string, Task<bool>> writeAsync);
}