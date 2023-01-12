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

    public class IOSynchronizationService : IIOSynchronizationService
    {
        private const string DefaultSyncFile = "__ofs.sync";

        private static readonly TimeSpan DefaultDelayBetweenChecks = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan DefaultDelayAfterWriteOperations = TimeSpan.FromMilliseconds(50);

        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IFileService _fileService;
        private readonly IDirectoryService _directoryService;

        private readonly AsyncLock _asyncLock = new AsyncLock();
        private readonly Dictionary<string, string> _basePathsCache = new Dictionary<string, string>();

        private readonly Dictionary<string, FileSystemWatcher> _fileSystemWatchers = new Dictionary<string, FileSystemWatcher>();
        private readonly Dictionary<string, string> _syncFilesCache = new Dictionary<string, string>();

        private readonly ConcurrentDictionary<string, Func<string, Task<bool>>> _readingCallbacks = new ConcurrentDictionary<string, Func<string, Task<bool>>>();
        private readonly ConcurrentDictionary<string, Func<string, Task<bool>>> _writingCallbacks = new ConcurrentDictionary<string, Func<string, Task<bool>>>();

        private readonly HashSet<string> _syncFilesInRead = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public IOSynchronizationService(IFileService fileService, IDirectoryService directoryService)
        {
            ArgumentNullException.ThrowIfNull(fileService);
            ArgumentNullException.ThrowIfNull(directoryService);

            _fileService = fileService;
            _directoryService = directoryService;

            DelayBetweenChecks = DefaultDelayBetweenChecks;
            DelayAfterWriteOperations = DefaultDelayAfterWriteOperations;
        }

        public TimeSpan DelayBetweenChecks { get; set; }

        public TimeSpan DelayAfterWriteOperations { get; set; }

        public event EventHandler<PathEventArgs>? RefreshRequired;

        public IDisposable AcquireReadLock(string path)
        {
            var scopeManager = GetScopeManager(true, path);
            scopeManager.ScopeObject.Lock();

            return scopeManager;
        }

        public IDisposable AcquireWriteLock(string path, bool notifyOnRelease = true)
        {
            var scopeManager = GetScopeManager(false, path);

            var scopeObject = scopeManager.ScopeObject;
            scopeObject.NotifyOnRelease = notifyOnRelease;
            scopeObject.Lock();

            return scopeManager;
        }

        public async Task StartWatchingForChangesAsync(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                var basePath = GetBasePath(path);

                using (await _asyncLock.LockAsync())
                {
#pragma warning disable IDISP001 // Dispose created.
                    if (!_fileSystemWatchers.TryGetValue(path, out var fileSystemWatcher))
                    {
                        Log.Debug($"Start watching path '{path}'");

                        var syncFile = GetSyncFileByPath(path);
                        syncFile = Path.GetFileName(syncFile);

                        fileSystemWatcher = CreateFileSystemWatcher(basePath, syncFile);
                        _fileSystemWatchers[path] = fileSystemWatcher;
                    }
#pragma warning restore IDISP001 // Dispose created.
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to start watching path '{path}'");
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

                    if (_fileSystemWatchers.TryGetValue(path, out var fileSystemWatcher))
                    {
                        Log.Debug($"Stop watching path '{path}'");

                        fileSystemWatcher.Changed -= OnFileSystemWatcherChanged;

                        fileSystemWatcher.EnableRaisingEvents = false;
                        fileSystemWatcher.Dispose();

                        _fileSystemWatchers.Remove(path);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to stop watching path '{path}'");
            }
        }

        public async Task ExecuteReadingAsync(string projectLocation, Func<string, Task<bool>> readAsync)
        {
            Argument.IsNotNullOrWhitespace(() => projectLocation);

            try
            {
                using (var scopeManager = GetScopeManager(true, projectLocation))
                {
                    var requiresStartReading = true;

                    Action action = () =>
                    {
                        requiresStartReading = !_readingCallbacks.ContainsKey(projectLocation);
                        _readingCallbacks[projectLocation] = readAsync;
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
                        await ExecutePendingReadingAsync(projectLocation);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to execute reading task for '{projectLocation}'");
                throw;
            }
        }

        public async Task ExecuteWritingAsync(string projectLocation, Func<string, Task<bool>> writeAsync)
        {
            Argument.IsNotNullOrWhitespace(() => projectLocation);

            try
            {
                using (var scopeManager = GetScopeManager(false, projectLocation))
                {
                    var requiresStartWriting = true;

                    Action action = () =>
                    {
                        requiresStartWriting = !_writingCallbacks.ContainsKey(projectLocation);
                        _writingCallbacks[projectLocation] = writeAsync;
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
                        await ExecutePendingWritingAsync(projectLocation);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to execute writing task for '{projectLocation}'");
                throw;
            }
        }

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

                    await Task.Delay(DelayBetweenChecks);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to execute pending reading for '{path}'");
                throw;
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

                    await Task.Delay(DelayBetweenChecks);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to execute pending writing for '{path}'");
                throw;
            }
        }

        protected internal string GetSyncFileByPath(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            if (_syncFilesCache.TryGetValue(path, out var syncFile))
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

            if (_basePathsCache.TryGetValue(path, out var basePath))
            {
                return basePath;
            }

            basePath = _directoryService.Exists(path) ? path : (Path.GetDirectoryName(path) ?? ".");

            _basePathsCache[path] = basePath;
            return basePath;
        }

        private FileSystemWatcher CreateFileSystemWatcher(string basePath, string fileSystemWatcherFilter)
        {
            var fileSystemWatcher = new FileSystemWatcher(basePath, fileSystemWatcherFilter)
            {
                NotifyFilter = NotifyFilters.LastWrite
            };

            fileSystemWatcher.Changed += OnFileSystemWatcherChanged;

            fileSystemWatcher.EnableRaisingEvents = true;

            return fileSystemWatcher;
        }

#pragma warning disable AvoidAsyncVoid
        private async void OnFileSystemWatcherChanged(object sender, FileSystemEventArgs e)
#pragma warning restore AvoidAsyncVoid
        {
            try
            {
                Log.Debug($"Received file watcher event '{e.FullPath} => {e.ChangeType}'");

                var fileName = e.FullPath;
                var paths = GetPathsBySyncFile(fileName);

                foreach (var path in paths)
                {
                    var readingScopeName = GetScopeName(path, false);
                    var readingScopeExists = ScopeManager<FileLockScope>.ScopeExists(readingScopeName);

                    if (_writingCallbacks.ContainsKey(path))
                    {
                        await ExecuteWritingIfPossibleAsync(path);
                    }

                    if (e.ChangeType.HasFlag(WatcherChangeTypes.Deleted) || e.ChangeType.HasFlag(WatcherChangeTypes.Created))
                    {
                        continue;
                    }

                    if (_readingCallbacks.ContainsKey(path))
                    {
                        await ExecuteReadingIfPossibleAsync(path);
                    }

                    // Note: we should not raise RefreshRequired when reading scope still exists
                    if (!readingScopeExists)
                    {
                        RefreshRequired?.Invoke(this, new PathEventArgs(path));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to handle FileSystemWatcher event e.ChangeType: '{e.ChangeType}', e.FullPath: '{e.FullPath}'");
            }
        }

        private async Task ExecuteReadingIfPossibleAsync(string path)
        {
            Func<string, Task<bool>>? read;

            var syncFile = GetSyncFileByPath(path);
            using (await _asyncLock.LockAsync())
            {
                if (!_readingCallbacks.TryRemove(path, out read))
                {
                    return;
                }
            }

            if (path.EqualsIgnoreCase(syncFile) && _syncFilesInRead.Contains(syncFile))
            {
                return;
            }

            _syncFilesInRead.Add(syncFile);

            bool succeeded;

            try
            {
                using (var scopeManager = GetScopeManager(true, path))
                {
                    var scopeObject = scopeManager.ScopeObject;
                    succeeded = scopeObject.Lock();

                    if (succeeded)
                    {
                        Log.Debug($"Executing read actions from path '{path}'");

                        try
                        {
                            succeeded = await read(path);
                        }
                        catch (Exception readException)
                        {
                            Log.Warning(readException, $"Fatal error in executing reading for '{path}': '{readException.Message}'");

                            throw Log.ErrorAndCreateException(message => new IOSynchronizationException(message, readException), $"Fatal error in executing reading for '{path}'");
                        }

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
            // Note: if we will swallow any other exception we will get endless loop
            catch (IOException ex)
            {
                var hResult = (uint)ex.GetHResult();
                if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
                {
                    // Note: no need to try again
                    throw;
                }

                Log.Warning(ex, $"Reading from '{path}' failed, adding enqueued action back in the queue");

                succeeded = false;
            }
            finally
            {
                _syncFilesInRead.Remove(syncFile);
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
            Func<string, Task<bool>>? write;

            using (await _asyncLock.LockAsync())
            {
                if (!_writingCallbacks.TryRemove(path, out write))
                {
                    return;
                }
            }

            bool succeeded;

            try
            {
                using (var scopeManager = GetScopeManager(false, path))
                {
                    var scopeObject = scopeManager.ScopeObject;
                    succeeded = scopeObject.Lock();

                    if (succeeded)
                    {
                        Log.Debug($"Executing write actions to path '{path}'");

                        try
                        {
                            succeeded = await write(path);
                        }
                        catch (Exception readException)
                        {
                            Log.Warning(readException, $"Fatal error in executing writing for '{path}': '{readException.Message}'");

                            throw Log.ErrorAndCreateException(message => new IOSynchronizationException(message, readException), $"Fatal error in executing writing for '{path}'");
                        }

                        if (!succeeded)
                        {
                            Log.Debug($"Failed to execute write actions to path '{path}', will retry");
                        }
                        else
                        {
                            var delay = DelayAfterWriteOperations;

                            Log.Debug($"Succeeded to execute write actions to path '{path}', using a delay of '{delay}'");

                            // Sometimes we need a bit of delay in order to write files to disk
                            await Task.Delay(delay);

                            scopeObject.WriteDummyContent();
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                var hResult = (uint)ex.GetHResult();
                if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
                {
                    // Note: no need to try again
                    throw;
                }

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

        private ScopeManager<FileLockScope> GetScopeManager(bool isReadScope, string path)
        {
            var scopeName = GetScopeName(path, !isReadScope);
            var syncFile = GetSyncFileByPath(path);

            return !string.Equals(path, syncFile)
                    ? ScopeManager<FileLockScope>.GetScopeManager(scopeName, () => new FileLockScope(isReadScope, syncFile, _fileService))
                    : ScopeManager<FileLockScope>.GetScopeManager(scopeName, () => FileLockScope.DummyLock);
        }
    }
}
