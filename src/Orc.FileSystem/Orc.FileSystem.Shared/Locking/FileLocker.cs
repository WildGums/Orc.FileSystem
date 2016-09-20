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
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly FileLocker _existingLocker;
        private readonly int _uniqueId = UniqueIdentifierHelper.GetUniqueIdentifier<FileLocker>();

        private Dictionary<FileInfo, FileStream> _lockedFiles = new Dictionary<FileInfo, FileStream>();

        public FileLocker(FileLocker existingLocker)
        {
            _existingLocker = existingLocker;
        }

        public void Dispose()
        {
            lock (_lockedFiles)
            {
                if (_lockedFiles != null)
                {
                    foreach (var lockedFile in _lockedFiles)
                    {
                        var fileStream = lockedFile.Value;
                        fileStream.Close();
                        fileStream.Dispose();
                    }

                    _lockedFiles.Clear();
                    _lockedFiles = null;
                }
            }
        }

        public Task LockFilesAsync(params string[] files)
        {
            if (_existingLocker != null)
            {
                return _existingLocker.LockFilesAsync(files);
            }

            var fileInfos = files.Select(x => new FileInfo(x)).ToArray();
            return LockFilesAsync(fileInfos);
        }

        public async Task LockFilesAsync(params FileInfo[] files)
        {
            if (_existingLocker != null)
            {
                await _existingLocker.LockFilesAsync(files);
                return;
            }

            Log.Debug($"[{_uniqueId}] Ensuring that the following files are not busy");

            foreach (var file in files)
            {
                Log.Debug($"[{_uniqueId}]  * {file.FullName}");
            }

            await files.EnsureFilesNotBusyAsync();

            foreach (var file in files.Where(x => x.Exists))
            {
                await LockFileAsync(file);
            }
        }

        public IDisposable UnlockTemporarily(string file)
        {
            var fileInfo = new FileInfo(file);
            return UnlockTemporarily(fileInfo);
        }

        public IDisposable UnlockTemporarily(FileInfo fileInfo)
        {
            if (_existingLocker != null)
            {
                return _existingLocker.UnlockTemporarily(fileInfo);
            }

            lock (_lockedFiles)
            {
                var existingLockedFile = GetExistingLockedFile(fileInfo);
                if (existingLockedFile == null)
                {
                    return new DisposableToken<object>(fileInfo, x => { }, x => { });
                }

                return new DisposableToken<object>(existingLockedFile, x =>
                {
                    Log.Debug($"[{_uniqueId}] Temporarily unlocking file '{fileInfo}'");

                    _lockedFiles[existingLockedFile].Dispose();
                },
                    async x =>
                    {
                        await LockFileAsync(fileInfo);
                    });
            }
        }

        private async Task LockFileAsync(FileInfo fileInfo)
        {
            Log.Debug($"[{_uniqueId}] Locking file '{fileInfo}' again after a temporarily unlock");

            var retryCounter = 0;

            var finalFileInfo = GetExistingLockedFile(fileInfo.FullName);
            if (finalFileInfo == null)
            {
                finalFileInfo = fileInfo;
            }

            if (!finalFileInfo.Exists)
            {
                Log.Debug($"[{_uniqueId}] Failed to lock file '{fileInfo.FullName}', it doesn't exist");
                return;
            }

            while (true)
            {
                retryCounter++;

                try
                {
                    var lockedFile = finalFileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None);

                    lock (_lockedFiles)
                    {
                        _lockedFiles[finalFileInfo] = lockedFile;
                    }

                    Log.Debug($"[{_uniqueId}] Locked file '{fileInfo.FullName}' after '{retryCounter}' tries");

                    break;
                }
                catch (Exception ex)
                {
                    var message = $"[{_uniqueId}] Failed to lock file '{fileInfo.FullName}', tried '{retryCounter}' time(s)";

                    if (retryCounter > 5)
                    {
                        Log.Error(ex, message);
                        throw;
                    }

                    Log.Debug(ex, message);
                }

                await TaskShim.Delay(10);
            }
        }

        private FileInfo GetExistingLockedFile(FileInfo filePath)
        {
            return GetExistingLockedFile(filePath.FullName);
        }

        private FileInfo GetExistingLockedFile(string filePath)
        {
            var existingLockedFile = (from lockedFile in _lockedFiles
                                      where filePath.EqualsIgnoreCase(lockedFile.Key.FullName)
                                      select lockedFile.Key).FirstOrDefault();
            return existingLockedFile;
        }
    }
}