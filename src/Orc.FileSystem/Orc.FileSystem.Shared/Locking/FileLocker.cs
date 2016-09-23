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

    public class FileLocker : IDisposable
    {
        #region Fields
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<string, FileStream> Locks = new Dictionary<string, FileStream>(StringComparer.InvariantCultureIgnoreCase);
        private static readonly Dictionary<string, int> LockCounts = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

        private readonly FileLocker _existingLocker;
        private readonly int _uniqueId = UniqueIdentifierHelper.GetUniqueIdentifier<FileLocker>();
 
        private readonly HashSet<string> _internalLocks = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        private static readonly AsyncLock AsyncLock = new AsyncLock();

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

            ReleaseLockFiles();
            _isDisposed = true;
        }

        private void ReleaseLockFiles()
        {
            lock (Locks)
            {
                Log.Debug("Releasing locked files");

                foreach (var lockFile in _internalLocks.ToList())
                {
                    int count;
                    LockCounts.TryGetValue(lockFile, out count);

                    _internalLocks.Remove(lockFile);

                    if (count > 0)
                    {
                        count --;
                    }

                    FileStream lockStream;
                    if (count <= 0 && Locks.TryGetValue(lockFile, out lockStream))
                    {
                        lockStream.Close();
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

        #region Methods
        public async Task LockFilesAsync(params string[] files)
        {           
            if (_existingLocker != null)
            {
                await _existingLocker.LockFilesAsync(files);
                return;
            }

            using (await AsyncLock.LockAsync())
            {
                var newLockFiles = files.Where(x => !x.EndsWith(".lock", StringComparison.InvariantCultureIgnoreCase)).Select(x => x + ".lock");
                string[] fileNames;

                lock (Locks)
                {
                    //fileNames = newLockFiles.Except(_internalLocks,  StringComparer.InvariantCultureIgnoreCase).ToArray();

                    // Note: instead of adding new locked files better to release already locked ones and lock them again combined with the new ones
                    //       I think it should prevent hangings in concurrent applications
                    fileNames = newLockFiles.Union(_internalLocks,  StringComparer.InvariantCultureIgnoreCase).ToArray();
                    ReleaseLockFiles();
                }

                Log.Debug($"[{_uniqueId}] Creating and locking following files");
                foreach (var file in fileNames)
                {
                    Log.Debug($"[{_uniqueId}]  * {file}");
                }

                var continueLoop = true;
                while (continueLoop)
                {
                    var lockedFiles = TryCreateAndLockFiles(fileNames);
                    if (lockedFiles == null)
                    {
                        await TaskShim.Delay(10);
                        continue;
                    }

                    lock (Locks)
                    {
                        foreach (var fileName in fileNames)
                        {
                            _internalLocks.Add(fileName);

                            int count;
                            LockCounts.TryGetValue(fileName, out count);
                            count++;
                            LockCounts[fileName] = count;

                            FileStream stream;
                            if (lockedFiles.TryGetValue(fileName, out stream) && stream != null)
                            {
                                Locks[fileName] = stream;
                            }
                        }

                        continueLoop = false;
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
                    var fileStream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    File.SetAttributes(fileName, FileAttributes.Hidden);

                    result[fileName] = fileStream;
                }
                catch(IOException)
                {
                    foreach (var fileStream in result.Values.Where(x => x!=null))
                    {
                        fileStream.Close();
                        fileStream.Dispose();
                    }

                    result.Clear();
                    result = null;

                    break;
                }
                catch (Exception)
                {
                    foreach (var fileStream in result.Values.Where(x => x != null))
                    {
                        fileStream.Close();
                        fileStream.Dispose();
                    }

                    result.Clear();

                    throw;
                }
            }

            return result;
        }

        [ObsoleteEx(RemoveInVersion = "2.0", TreatAsErrorFromVersion = "1.0")]
        public Task LockFilesAsync(params FileInfo[] files)
        {
            if (_existingLocker != null)
            {
                return _existingLocker.LockFilesAsync(files);
            }

            var fileInfos = files.Select(x => x.FullName).ToArray();
            return LockFilesAsync(fileInfos);
        }


        [ObsoleteEx(RemoveInVersion = "2.0", TreatAsErrorFromVersion = "1.0")]
        public IDisposable UnlockTemporarily(string file)
        {
            if (_existingLocker != null)
            {
                return _existingLocker.UnlockTemporarily(file);
            }

            return new DisposableToken<object>(file, x => { }, x => { });
        }

        [ObsoleteEx(RemoveInVersion = "2.0", TreatAsErrorFromVersion = "1.0")]
        public IDisposable UnlockTemporarily(FileInfo fileInfo)
        {
            return UnlockTemporarily(fileInfo.FullName);
        }
        #endregion
    }
}