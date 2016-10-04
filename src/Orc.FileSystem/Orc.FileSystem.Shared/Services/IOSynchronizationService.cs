﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectIOSynchronizationService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Catel;
    using Catel.Logging;
    using Catel.Threading;
    using Orc.FileSystem;
    using Path = Catel.IO.Path;

    public class IOSynchronizationService : IIOSynchronizationService
    {
        #region Constants
        private const string DefaultSyncFile = "__ofs.sync";
        private static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(250);
        #endregion

        #region #region Fields
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IFileService _fileService;

        private readonly AsyncLock _asyncLock = new AsyncLock();
        private readonly Dictionary<string, string> _basePathsCache = new Dictionary<string, string>();

        private readonly Dictionary<string, FileSystemWatcher> _fileSystemWatchers = new Dictionary<string, FileSystemWatcher>();
        private readonly Dictionary<string, string> _refreshFileCache = new Dictionary<string, string>();

        private readonly ConcurrentDictionary<string, Func<string, Task<bool>>> _readingCallbacks = new ConcurrentDictionary<string, Func<string, Task<bool>>>();
        private readonly ConcurrentDictionary<string, Func<string, Task<bool>>> _writingCallbacks = new ConcurrentDictionary<string, Func<string, Task<bool>>>();
        #endregion

        #region Constructors
        public IOSynchronizationService(IFileService fileService)
        {
            Argument.IsNotNull(() => fileService);

            _fileService = fileService;
        }
        #endregion

        #region IProjectIOSynchronizationService members
        public event EventHandler<PathEventArgs> RefreshRequired;

        public async Task StartWatchingForChangesAsync(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                var basePath = GetBasePath(path);

                using (await _asyncLock.LockAsync())
                {
                    FileSystemWatcher fileSystemWatcher;
                    if (!_fileSystemWatchers.TryGetValue(path, out fileSystemWatcher))
                    {
                        Log.Debug($"Start watching path '{path}'");

                        var refreshFile = GetRefreshFileByPath(path);
                        refreshFile = Path.GetFileName(refreshFile);

                        DeleteRefreshFile(path);

                        fileSystemWatcher = CreateFileSystemWatcher(basePath, refreshFile);
                        _fileSystemWatchers[path] = fileSystemWatcher;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to start watching path '{path}'");
            }
        }

        public async Task StopWatchingForChangesAsync(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                using (await _asyncLock.LockAsync())
                {
                    _basePathsCache.Remove(path);
                    _refreshFileCache.Remove(path);

                    FileSystemWatcher fileSystemWatcher;
                    if (_fileSystemWatchers.TryGetValue(path, out fileSystemWatcher))
                    {
                        Log.Debug($"Stop watching path '{path}'");

                        fileSystemWatcher.Created -= OnFileSystemWatcherChanged;
                        fileSystemWatcher.Changed -= OnFileSystemWatcherChanged;
                        fileSystemWatcher.Deleted -= OnFileSystemWatcherChanged;

                        fileSystemWatcher.EnableRaisingEvents = false;
                        fileSystemWatcher.Dispose();

                        _fileSystemWatchers.Remove(path);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to stop watching path '{path}'");
            }
        }

        public async Task ExecuteReadingAsync(string path, Func<string, Task<bool>> readAsync)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                bool requiresStartReading;

                using (await _asyncLock.LockAsync())
                {
                    requiresStartReading = !_readingCallbacks.ContainsKey(path);

                    _readingCallbacks[path] = readAsync;
                }

                if (requiresStartReading)
                {
                    ExecutePendingReading(path);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to execute reading task for '{path}'");
            }
        }

        public async Task ExecuteWritingAsync(string path, Func<string, Task<bool>> writeAsync)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                bool requiresStartWriting;
                using (await _asyncLock.LockAsync())
                {
                    requiresStartWriting = !_writingCallbacks.ContainsKey(path);

                    _writingCallbacks[path] = writeAsync;
                }

                if (requiresStartWriting)
                {
                    ExecutePendingWriting(path);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to execute writing task for '{path}'");
            }
        }
        #endregion

        #region Methods
        protected virtual string ResolveObservedFileName(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            var basePath = GetBasePath(path);

            return Path.Combine(basePath, DefaultSyncFile);
        }

        private async void ExecutePendingReading(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                while (_readingCallbacks.ContainsKey(path))
                {
                    await ExecuteReadingIfPossibleAsync(path);

                    await TaskShim.Delay(DefaultDelay);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to execute pending reading for '{path}'");
            }
        }

        private async void ExecutePendingWriting(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                while (_writingCallbacks.ContainsKey(path))
                {
                    await ExecuteWritingIfPossibleAsync(path);

                    await TaskShim.Delay(DefaultDelay);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to execute pending writing for '{path}'");
            }
        }

        private string GetRefreshFileByPath(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            string refreshFile;
            if (_refreshFileCache.TryGetValue(path, out refreshFile))
            {
                return refreshFile;
            }

            refreshFile = ResolveObservedFileName(path);

            _refreshFileCache[path] = refreshFile;

            return refreshFile;
        }

        private List<string> GetPathsByRefreshFile(string fullPath)
        {
            return _refreshFileCache.Where(x => x.Value.EqualsIgnoreCase(fullPath))
                .Select(x => x.Key).Distinct().ToList();
        }

        private string GetBasePath(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            string basePath;
            if (_basePathsCache.TryGetValue(path, out basePath))
            {
                return basePath;
            }

            basePath = _fileService.Exists(path)
                ? Path.GetParentDirectory(path)
                : path;

            _basePathsCache[path] = basePath;
            return basePath;
        }

        private FileSystemWatcher CreateFileSystemWatcher(string basePath, string fileSystemWatcherFilter)
        {
            var fileSystemWatcher = new FileSystemWatcher(basePath, fileSystemWatcherFilter)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            fileSystemWatcher.Created += OnFileSystemWatcherChanged;
            fileSystemWatcher.Changed += OnFileSystemWatcherChanged;
            fileSystemWatcher.Deleted += OnFileSystemWatcherChanged;

            fileSystemWatcher.EnableRaisingEvents = true;

            return fileSystemWatcher;
        }

        private async void OnFileSystemWatcherChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                Log.Debug($"Received file watcher event '{e.FullPath} => {e.ChangeType}'");

                var fileName = e.FullPath;
                var paths = GetPathsByRefreshFile(fileName);

                foreach (var path in paths)
                {
                    if (_writingCallbacks.ContainsKey(path))
                    {
                        await ExecuteWritingIfPossibleAsync(path);
                    }

                    if (e.ChangeType.HasFlag(WatcherChangeTypes.Deleted))
                    {
                        continue;
                    }

                    if (_readingCallbacks.ContainsKey(path))
                    {
                        await ExecuteReadingIfPossibleAsync(path);
                    }
                    else
                    {
                        RefreshRequired?.Invoke(this, new PathEventArgs(path));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to handle FileSystemWatcher event e.ChangeType: '{e.ChangeType}', e.FullPath: '{e.FullPath}'");
            }
        }

        private async Task ExecuteReadingIfPossibleAsync(string path)
        {
            using (await _asyncLock.LockAsync())
            {
                Func<string, Task<bool>> read;
                if (!_readingCallbacks.TryGetValue(path, out read))
                {
                    return;
                }

                var refreshFile = GetRefreshFileByPath(path);
                if (!_fileService.Exists(refreshFile))
                {
                    return;
                }

                FileSystemWatcher fileSystemWatcher;
                _fileSystemWatchers.TryGetValue(path, out fileSystemWatcher);

                var success = false;
                FileStream fileStream = null;

                try
                {
                    // Note: no need to lock file in FG-like projects (single file)
                    if (!path.EqualsIgnoreCase(refreshFile))
                    {
                        try
                        {
                            fileStream = _fileService.Open(refreshFile, FileMode.Open, FileAccess.Read, FileShare.None);
                        }
                        catch (IOException)
                        {
                            return;
                        }
                    }

                    success = await read(path);
                    if (!success)
                    {
                        return;
                    }

                    _readingCallbacks.TryRemove(path, out read);
                }
                finally
                {
                    fileStream?.Dispose();

                    if (success)
                    {
                        DeleteRefreshFile(path);
                    }
                }
            }
        }

        private async Task ExecuteWritingIfPossibleAsync(string path)
        {
            using (await _asyncLock.LockAsync())
            {
                Func<string, Task<bool>> write;
                if (!_writingCallbacks.TryGetValue(path, out write))
                {
                    return;
                }

                var refreshFile = GetRefreshFileByPath(path);

                FileStream fileStream = null;
                try
                {
                    // Note: no need to lock file in FG-like projects (single file)
                    if (!path.EqualsIgnoreCase(refreshFile))
                    {
                        try
                        {
                            DeleteRefreshFile(path);
                            fileStream = _fileService.OpenWrite(refreshFile);
                        }
                        catch (IOException)
                        {
                            return;
                        }
                    }

                    if (!await write(path))
                    {
                        return;
                    }

                    // Note: writing dummy data for FileSystemWatcher
                    fileStream?.WriteByte(0);

                    _writingCallbacks.TryRemove(path, out write);
                }
                finally
                {
                    fileStream?.Dispose();
                }
            }
        }

        private void DeleteRefreshFile(string path)
        {
            var refreshFile = GetRefreshFileByPath(path);

            try
            {
                if (_fileService.Exists(refreshFile) && !path.EqualsIgnoreCase(refreshFile))
                {
                    _fileService.Delete(refreshFile);
                }
            }
            catch (IOException)
            {
            }
        }
        #endregion
    }
}