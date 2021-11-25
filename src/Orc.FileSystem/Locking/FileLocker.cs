// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileLocker.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Catel;
    using Catel.Logging;
    using Catel.Threading;

    public sealed class FileLocker : IDisposable
    {
        #region Fields
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<string, FileStream> Locks = new Dictionary<string, FileStream>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> LockCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly AsyncLock AsyncLock = new AsyncLock();

        private readonly FileLocker _existingLocker;
        private readonly int _uniqueId = UniqueIdentifierHelper.GetUniqueIdentifier<FileLocker>();

        private readonly HashSet<string> _internalLocks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private bool _isDisposed;
        #endregion

        #region Constructors
        public FileLocker(FileLocker existingLocker)
        {
            _existingLocker = existingLocker;
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            ReleaseLockedFiles();

            _isDisposed = true;
        }
        #endregion

        #region Methods
        public Task LockFilesAsync(params string[] files)
        {
            return LockFilesAsync(TimeSpan.FromSeconds(5), files);
        }

        public async Task LockFilesAsync(TimeSpan timeout, params string[] files)
        {
            if (_existingLocker is not null)
            {
                await _existingLocker.LockFilesAsync(timeout, files);
                return;
            }

            using (await AsyncLock.LockAsync())
            {
                var newLockFiles = files.Where(x => !x.EndsWith(".lock", StringComparison.OrdinalIgnoreCase)).Select(x => x + ".lock");
                string[] fileNames;

                lock (Locks)
                {
                    // Note: instead of adding new locked files better to release already locked ones and lock them again combined with the new ones
                    //       I think it should prevent hangings in concurrent applications
                    fileNames = newLockFiles.Union(_internalLocks, StringComparer.OrdinalIgnoreCase).ToArray();
                    ReleaseLockedFiles();
                }

                Log.Debug($"[{_uniqueId}] Creating and locking following files");
                foreach (var file in fileNames)
                {
                    Log.Debug($"[{_uniqueId}]  * {file}");
                }

                var continueLoop = true;

                var timerHandler = new TimerCallback(x =>
                {
                    continueLoop = false;

                    Log.Warning("Locking files has interrupted due to timeout");
                });

                using (var timer = new Timer(timerHandler, null, timeout, Timeout.InfiniteTimeSpan))
                {
                    while (continueLoop)
                    {
                        var lockedFiles = TryCreateAndLockFiles(fileNames);
                        if (lockedFiles is null && continueLoop)
                        {
                            await TaskShim.Delay(10);
                            continue;
                        }

                        if (lockedFiles is null)
                        {
                            continue;
                        }

                        lock (Locks)
                        {
                            foreach (var fileName in fileNames)
                            {
                                _internalLocks.Add(fileName);

                                LockCounts.TryGetValue(fileName, out var count);
                                count++;
                                LockCounts[fileName] = count;

                                if (lockedFiles.TryGetValue(fileName, out var stream) && stream is not null)
                                {
                                    Locks[fileName] = stream;
                                }
                            }

                            continueLoop = false;
                        }
                    }
                }
            }
        }

        private static Dictionary<string, FileStream> TryCreateAndLockFiles(string[] fileNames)
        {
            var result = new Dictionary<string, FileStream>();

            foreach (var fileName in fileNames)
            {
                try
                {
                    result[fileName] = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    File.SetAttributes(fileName, FileAttributes.Hidden);
                }
                catch (IOException)
                {
                    foreach (var fileStream in result.Values.Where(x => x is not null))
                    {
                        fileStream.Dispose();
                    }

                    result.Clear();
                    result = null;

                    break;
                }
                catch (Exception)
                {
                    foreach (var fileStream in result.Values.Where(x => x is not null))
                    {
                        fileStream.Dispose();
                    }

                    result.Clear();

                    throw;
                }
            }

            return result;
        }

        private void ReleaseLockedFiles()
        {
            lock (Locks)
            {
                Log.Debug("Releasing locked files");

                foreach (var lockFile in _internalLocks.ToList())
                {
                    LockCounts.TryGetValue(lockFile, out var count);

                    _internalLocks.Remove(lockFile);

                    if (count > 0)
                    {
                        count--;
                    }

                    if (count <= 0 && Locks.TryGetValue(lockFile, out var lockStream))
                    {
                        lockStream.Dispose();

                        Locks.Remove(lockFile);

                        Log.Debug($"'{lockFile}' released");
                    }

                    if (count <= 0 && File.Exists(lockFile))
                    {
                        try
                        {
                            File.Delete(lockFile);

                            Log.Debug($"'{lockFile}' deleted");
                        }
                        catch (Exception ex)
                        {
                            // it is not a reason for crashing the app
                            Log.Warning(ex, $"Failed to delete '{lockFile}'");
                        }
                    }

                    if (count > 0)
                    {
                        LockCounts[lockFile] = count;
                    }
                    else
                    {
                        LockCounts.Remove(lockFile);
                    }
                }
            }
        }
        #endregion
    }
}
