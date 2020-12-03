// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IProjectIOSynchronizationService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.Threading.Tasks;

    public interface IIOSynchronizationService
    {
        Task StartWatchingForChangesAsync(string path);
        Task StopWatchingForChangesAsync(string path);

        Task ExecuteReadingAsync(string projectLocation, Func<string, Task<bool>> readAsync);
        Task ExecuteWritingAsync(string projectLocation, Func<string, Task<bool>> writeAsync);

        event EventHandler<PathEventArgs> RefreshRequired;
        TimeSpan DelayBetweenChecks { get; set; }
        TimeSpan DelayAfterWriteOperations { get; set; }
        IDisposable AcquireReadLock(string path);
        IDisposable AcquireWriteLock(string path, bool notifyOnRelease = true);
    }
}
