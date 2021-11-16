// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileLockScope.cs" company="WildGums">
//   Copyright (c) 2008 - 2017 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using Catel;
    using Catel.IoC;
    using Catel.Logging;

    public class FileLockScope : Disposable, IFileLockScope
    {
        private readonly FileLockScopeContext _context;
        private readonly ISyncFileNameService _syncFileNameService;
        private readonly IFile _file;
        private readonly IDirectory _directory;

        #region Constants
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        #endregion

        #region Fields
        private readonly bool _isReadScope;
        private readonly object _lock = new ();
        private readonly string _syncFile;

        private Stream _stream;

        private int _lockAttemptCounter;
        #endregion

        #region Constructors
        public FileLockScope()
        {
            // DummyLock
        }

        public FileLockScope(FileLockScopeContext context, ISyncFileNameService syncFileNameService, IFileSystem fileSystem)
        {
            _context = context;
            _syncFileNameService = syncFileNameService;
            
            _syncFile = syncFileNameService.GetFileName(context);
            _isReadScope = context.IsReadScope ?? true;
            _file = fileSystem.File;
            _directory = fileSystem.Directory;
        }

        [ObsoleteEx]
        public FileLockScope(bool isReadScope, string syncFile, IFileService fileService)
            : this(GetContext(isReadScope, syncFile, fileService.GetServiceLocator()),
                fileService.GetServiceLocator().ResolveType<ISyncFileNameService>(), 
                fileService.GetServiceLocator().ResolveType<IFileSystem>())
        {
        }

        private static FileLockScopeContext GetContext(bool isReadScope, string syncFile, IServiceLocator serviceLocator)
        {
            var syncFileNameService = serviceLocator.ResolveType<ISyncFileNameService>();
            var context = syncFileNameService.FileLockScopeContextFromFile(syncFile);
            context.IsReadScope = isReadScope;

            return context;
        }
        #endregion

        #region Properties
        private bool HasStream
        {
            get
            {
                lock (_lock)
                {
                    return _stream is not null;
                }
            }
        }

        private bool IsDummyLock => string.IsNullOrWhiteSpace(_syncFile);

        public bool NotifyOnRelease { get; set; }
        #endregion

        #region Methods
        public void WriteDummyContent()
        {
            if (IsDummyLock)
            {
                return;
            }

            // Note: writing dummy data for FileSystemWatcher
            lock (_lock)
            {
                _stream?.WriteByte(0);
            }
        }

        protected virtual string[] GetOtherSyncFileNames()
        {
            var directory = _directory.GetParent(_syncFile);
            var searchFilter = _syncFileNameService.GetFileSearchFilter(_context);
            
            var allLockFiles = _directory.EnumerateFiles(directory.FullName, "searchFilter", SearchOption.TopDirectoryOnly);

            return allLockFiles.Where(x => x != _syncFile).ToArray();
        }

        protected virtual Stream CreateSyncStream()
        {
            var otherLockFiles = GetOtherSyncFileNames();
            if (otherLockFiles.Any() && !_isReadScope)
            {
                // Throw exception?
            }

            // Note: don't use _fileService because we don't want logging in case of failure
            var stream  = _file.Open(_syncFile, FileMode.Create, FileAccess.Write, FileShare.None);
            return stream;
        }

        public bool Lock()
        {
            lock (_lock)
            {
                if (IsDummyLock || HasStream)
                {
                    return true;
                }

                DeleteOrphanedFiles();

                try
                {
                    // Note: don't use _fileService because we don't want logging in case of failure
                    _stream = CreateSyncStream();

                    Log.Debug($"Locked synchronization file '{_syncFile}'");
                }
                catch (IOException ex)
                {
                    var hResult = (uint)ex.GetHResult();

                    if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
                    {
                        throw Log.ErrorAndCreateException<FileLockScopeException>(ex, $"Failed to lock synchronization file '{_syncFile}'");
                    }

                    if (_lockAttemptCounter > 0)
                    {
                        return false;
                    }

                    var processes = FileLockInfo.GetProcessesLockingFile(_syncFile);
                    if (processes is null || !processes.Any())
                    {
                        Log.Debug(ex, $"First attempt to lock synchronization file '{_syncFile}' was unsuccessful. " +
                                     "Possibly locked by unknown application. Will keep retrying in the background.");
                    }
                    else
                    {
                        Log.Debug($"First attempt to lock synchronization file '{_syncFile}' was unsuccessful. " +
                                 $"Locked by: {string.Join(", ", processes)}. Will keep retrying in the background.");
                    }

                    return false;
                }
                finally
                {
                    _lockAttemptCounter++;
                }

                return true;
            }
        }

        private void DeleteOrphanedFiles()
        {
            _syncFileNameService.GetFileSearchFilter(_context);
        }

        public void Unlock()
        {
            if (IsDummyLock)
            {
                return;
            }

            if (NotifyOnRelease)
            {
                WriteDummyContent();
            }

            if (_isReadScope || _context.IsReadScope is not null)
            {
                // Note: deleting sync file before releasing, in order to prevent locking by another application
                DeleteSyncFile();
            }

            if (_stream is not null)
            {
                _stream.Dispose();
                _stream = null;

                Log.Debug($"Unlocked synchronization file '{_syncFile}'");
            }

            _lockAttemptCounter = 0;
        }

        protected override void DisposeManaged()
        {
            Unlock();
        }

        private void DeleteSyncFile()
        {
            try
            {
                if (_file.Exists(_syncFile))
                {
                    _file.Delete(_syncFile);

                    Log.Debug($"Deleted synchronization file '{_syncFile}'");
                }
            }
            catch (IOException ex)
            {
                var processes = FileLockInfo.GetProcessesLockingFile(_syncFile);
                if (processes is null || !processes.Any())
                {
                    Log.Warning(ex, $"Failed to delete synchronization file '{_syncFile}'");
                }
                else
                {
                    Log.Warning(ex, $"Failed to delete synchronization file '{_syncFile}' locked by: {string.Join(", ", processes)}");
                }
            }
        }
        #endregion
    }
}
