// --------------------------------------------------------------------------------------------------------------------
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
    using Catel.Scoping;
    using Catel.Threading;
    using Orc.FileSystem;
    using Path = Catel.IO.Path;

    public class IOSynchronizationService : IIOSynchronizationService
    {
        #region Constants
        private const string DefaultSyncFile = "__ofs.sync";

        private static readonly TimeSpan DefaultDelayBetweenChecks = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan DefaultDelayAfterWriteOperations = TimeSpan.FromMilliseconds(50);
        #endregion

        #region #region Fields
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IFileService _fileService;

        private readonly AsyncLock _asyncLock = new AsyncLock();
        private readonly Dictionary<string, string> _basePathsCache = new Dictionary<string, string>();

        private readonly Dictionary<string, FileSystemWatcher> _fileSystemWatchers = new Dictionary<string, FileSystemWatcher>();
        private readonly Dictionary<string, string> _syncFilesCache = new Dictionary<string, string>();

        private readonly ConcurrentDictionary<string, Func<string, Task<bool>>> _readingCallbacks = new ConcurrentDictionary<string, Func<string, Task<bool>>>();
        private readonly ConcurrentDictionary<string, Func<string, Task<bool>>> _writingCallbacks = new ConcurrentDictionary<string, Func<string, Task<bool>>>();
        #endregion

        #region Constructors
        public IOSynchronizationService(IFileService fileService)
        {
            Argument.IsNotNull(() => fileService);

            _fileService = fileService;

            DelayBetweenChecks = DefaultDelayBetweenChecks;
            DelayAfterWriteOperations = DefaultDelayAfterWriteOperations;
        }
        #endregion

        #region IIOSynchronizationService members
        public TimeSpan DelayBetweenChecks { get; set; }

        public TimeSpan DelayAfterWriteOperations { get; set; }

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

                        var syncFile = GetSyncFileByPath(path);
                        syncFile = Path.GetFileName(syncFile);

                        fileSystemWatcher = CreateFileSystemWatcher(basePath, syncFile);
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
                    _syncFilesCache.Remove(path);

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
                var scopeName = GetScopeName(path, false);
                var syncFile = GetSyncFileByPath(path);
                using (var scopeManager = ScopeManager<FileLockScope>.GetScopeManager(scopeName, () => new FileLockScope(true, syncFile, _fileService)))
                {
                    var requiresStartReading = true;

                    Action action = () =>
                    {
                        requiresStartReading = !_readingCallbacks.ContainsKey(path);
                        _readingCallbacks[path] = readAsync;
                    };

                    // If scope ref count <= 1, we are the first in this process to access this path
                    // and we need to await clearance
                    var requiresLock = scopeManager.RefCount <= 1;
                    if (requiresLock)
                    {
                        using (await _asyncLock.LockAsync())
                        {
                            action();
                        }
                    }
                    else
                    {
                        action();
                    }

                    if (requiresStartReading)
                    {
                        await ExecutePendingReadingAsync(path);
                    }
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
                var scopeName = GetScopeName(path, true);
                var syncFile = GetSyncFileByPath(path);
                using (var scopeManager = ScopeManager<FileLockScope>.GetScopeManager(scopeName, () => new FileLockScope(false, syncFile, _fileService)))
                {
                    var requiresStartWriting = true;

                    Action action = () =>
                    {
                        requiresStartWriting = !_writingCallbacks.ContainsKey(path);
                        _writingCallbacks[path] = writeAsync;
                    };

                    // If scope ref count <= 1, we are the first in this process to access this path
                    // and we need to await clearance
                    var requiresLock = scopeManager.RefCount <= 1;
                    if (requiresLock)
                    {
                        using (await _asyncLock.LockAsync())
                        {
                            action();
                        }
                    }
                    else
                    {
                        action();
                    }

                    if (requiresStartWriting)
                    {
                        await ExecutePendingWritingAsync(path);
                    }
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

        private async Task ExecutePendingReadingAsync(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                while (_readingCallbacks.ContainsKey(path))
                {
                    await ExecuteReadingIfPossibleAsync(path);

                    await TaskShim.Delay(DelayBetweenChecks);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to execute pending reading for '{path}'");
            }
        }

        private async Task ExecutePendingWritingAsync(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                while (_writingCallbacks.ContainsKey(path))
                {
                    await ExecuteWritingIfPossibleAsync(path);

                    await TaskShim.Delay(DelayBetweenChecks);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to execute pending writing for '{path}'");
            }
        }

        protected internal string GetSyncFileByPath(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            string syncFile;
            if (_syncFilesCache.TryGetValue(path, out syncFile))
            {
                return syncFile;
            }

            syncFile = ResolveObservedFileName(path);

            _syncFilesCache[path] = syncFile;

            return syncFile;
        }

        private List<string> GetPathsBySyncFile(string fullPath)
        {
            return _syncFilesCache.Where(x => x.Value.EqualsIgnoreCase(fullPath))
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
                var paths = GetPathsBySyncFile(fileName);

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
            Func<string, Task<bool>> read;

            var syncFile = GetSyncFileByPath(path);
            if (!_fileService.Exists(syncFile))
            {
                return;
            }

            using (await _asyncLock.LockAsync())
            {
                if (!_readingCallbacks.TryRemove(path, out read))
                {
                    return;
                }
            }

            var succeeded = true;

            try
            {
                var scopeName = GetScopeName(path, false);

                using (var scopeManager = ScopeManager<FileLockScope>.GetScopeManager(scopeName, () => new FileLockScope(true, syncFile, _fileService)))
                {
                    var scopeObject = scopeManager.ScopeObject;
                    if (!scopeObject.HasStream)
                    {
                        if (!scopeObject.Lock())
                        {
                            succeeded = false;
                        }
                    }

                    if (succeeded)
                    {
                        Log.Debug($"Executing read actions from path '{path}'");

                        succeeded = await read(path);
                        if (!succeeded)
                        {
                            Log.Debug($"Failed to execute read actions to path '{path}'");
                        }
                        else
                        {
                            Log.Debug($"Succeeded to execute read actions to path '{path}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Reading from '{path}' failed, adding enqueued action back in the queue");

                succeeded = false;
            }

            if (!succeeded)
            {
                using (await _asyncLock.LockAsync())
                {
                    _readingCallbacks.TryAdd(path, read);
                }
            }
        }

        private async Task ExecuteWritingIfPossibleAsync(string path)
        {
            Func<string, Task<bool>> write;

            using (await _asyncLock.LockAsync())
            {
                if (!_writingCallbacks.TryRemove(path, out write))
                {
                    return;
                }
            }

            var succeeded = true;

            try
            {
                var scopeName = GetScopeName(path, true);
                var syncFile = GetSyncFileByPath(path);

                using (var scopeManager = ScopeManager<FileLockScope>.GetScopeManager(scopeName, () => new FileLockScope(false, syncFile, _fileService)))
                {
                    var scopeObject = scopeManager.ScopeObject;
                    if (!scopeObject.HasStream)
                    {
                        if (!scopeObject.Lock())
                        {
                            succeeded = false;
                        }
                    }

                    if (succeeded)
                    {
                        Log.Debug($"Executing write actions to path '{path}'");

                        succeeded = await write(path);
                        if (!succeeded)
                        {
                            Log.Debug($"Failed to execute write actions to path '{path}'");
                        }
                        else
                        {
                            var delay = DelayAfterWriteOperations;

                            Log.Debug($"Succeeded to execute write actions to path '{path}', using a delay of '{delay}'");

                            // Sometimes we need a bit of delay in order to write files to disk
                            await TaskShim.Delay(delay);

                            scopeObject.WriteDummyContent();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Writing to '{path}' failed, adding enqueued action back in the queue");

                succeeded = false;
            }

            if (!succeeded)
            {
                using (await _asyncLock.LockAsync())
                {
                    _writingCallbacks.TryAdd(path, write);
                }
            }
        }

        private string GetScopeName(string path, bool isWriteLock)
        {
            var scopeName = $"{path}_" + (isWriteLock ? "write" : "read");
            return scopeName;
        }
        #endregion

        #region Nested classes
        private class FileLockScope : Disposable
        {
            private readonly object _lock = new object();
            private readonly bool _isReadScope;
            private readonly string _syncFile;
            private readonly IFileService _fileService;

            private FileStream _fileStream;

            public FileLockScope(bool isReadScope, string syncFile, IFileService fileService)
            {
                Argument.IsNotNullOrWhitespace(() => syncFile);
                Argument.IsNotNull(() => fileService);

                _isReadScope = isReadScope;
                _syncFile = syncFile;
                _fileService = fileService;
            }

            public bool HasStream
            {
                get
                {
                    lock (_lock)
                    {
                        return _fileStream != null;
                    }
                }
            }

            public void WriteDummyContent()
            {
                // Note: writing dummy data for FileSystemWatcher
                lock (_lock)
                {
                    _fileStream?.WriteByte(0);
                }
            }

            public bool Lock()
            {
                lock (_lock)
                {
                    if (HasStream)
                    {
                        return true;
                    }

                    var succeeded = true;

                    try
                    {
                        // Note: don't use _fileService because we don't want logging in case of failure
                        _fileStream = File.Open(_syncFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    }
                    catch (IOException)
                    {
                        succeeded = false;
                    }

                    return succeeded;
                }
            }

            public void Unlock()
            {
                if (_fileStream != null)
                {
                    _fileStream.Dispose();
                    _fileStream = null;
                }

                if (_isReadScope)
                {
                    DeleteSyncFile();
                }
            }

            private void DeleteSyncFile()
            {
                try
                {
                    if (_fileService.Exists(_syncFile))
                    {
                        _fileService.Delete(_syncFile);
                    }
                }
                catch (IOException ex)
                {
                    Log.Warning(ex, $"Failed to delete synchronization file '{_syncFile}'");
                }
            }

            protected override void DisposeManaged()
            {
                Unlock();
            }
        }
        #endregion
    }
}