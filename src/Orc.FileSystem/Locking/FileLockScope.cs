// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileLockScope.cs" company="WildGums">
//   Copyright (c) 2008 - 2017 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System.IO;
    using System.Linq;
    using Catel;
    using Catel.Logging;

    public class FileLockScope : Disposable
    {
        #region Constants
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Fields
        private readonly IFileService _fileService;
        private readonly bool _isReadScope;

        private readonly object _lock = new object();
        private readonly string _syncFile;

        private FileStream _fileStream;

        private int _lockAttemptCounter;
        #endregion

        #region Constructors
        public FileLockScope()
        {
            // DummyLock
        }

        public FileLockScope(bool isReadScope, string syncFile, IFileService fileService)
        {
            Argument.IsNotNullOrWhitespace(() => syncFile);
            Argument.IsNotNull(() => fileService);

            _isReadScope = isReadScope;
            _syncFile = syncFile;
            _fileService = fileService;
        }
        #endregion

        #region Properties
        private bool HasStream
        {
            get
            {
                lock (_lock)
                {
                    return _fileStream is not null;
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
                _fileStream?.WriteByte(0);
            }
        }

        public bool Lock()
        {
            lock (_lock)
            {
                if (IsDummyLock || HasStream)
                {
                    return true;
                }

                try
                {
                    // Note: don't use _fileService because we don't want logging in case of failure
                    _fileStream = File.Open(_syncFile, FileMode.Create, FileAccess.Write, _isReadScope ? FileShare.Delete : FileShare.None);

                    Log.Debug($"Locked synchronization file '{_syncFile}'");
                }
                catch (IOException ex)
                {
                    var hResult = (uint)ex.GetHResult();

                    if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
                    {
                        Log.Warning(ex, $"Failed to lock synchronization file '{_syncFile}'");

                        throw new FileLockScopeException($"Failed to lock synchronization file '{_syncFile}'", ex);
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

            if (_isReadScope)
            {
                // Note: deleting sync file before releasing, in order to prevent locking by another application
                DeleteSyncFile();
            }

            if (_fileStream is not null)
            {
                _fileStream.Dispose();
                _fileStream = null;

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
                if (_fileService.Exists(_syncFile))
                {
                    _fileService.Delete(_syncFile);

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
