namespace Orc.FileSystem;

using System;
using System.IO;
using System.Linq;
using Catel;
using Catel.Logging;

public class FileLockScope : Disposable
{
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

    private static readonly FileLockScope _dummyLock = new();

    private readonly IFileService? _fileService;
    private readonly bool _isReadScope;

    private readonly object _lock = new object();
    private readonly string? _syncFile;

#pragma warning disable IDISP006 // Implement IDisposable.
    private Stream? _stream;
#pragma warning restore IDISP006 // Implement IDisposable.

    private int _lockAttemptCounter;

    private FileLockScope()
    {
        // DummyLock
    }

    public FileLockScope(bool isReadScope, string syncFile, IFileService fileService)
    {
        Argument.IsNotNullOrWhitespace(() => syncFile);
        ArgumentNullException.ThrowIfNull(fileService);

        _isReadScope = isReadScope;
        _syncFile = syncFile;
        _fileService = fileService;
    }

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

    public static FileLockScope DummyLock
    {
        get
        {
            return _dummyLock;
        }
    }

    public bool NotifyOnRelease { get; set; }

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

    public bool Lock()
    {
        lock (_lock)
        {
            var syncFile = _syncFile;
            if (syncFile is null || IsDummyLock || HasStream)
            {
                return true;
            }

            try
            {
                // Note: don't use _fileService because we don't want logging in case of failure
                _stream?.Dispose();
                _stream = File.Open(syncFile, FileMode.Create, FileAccess.Write, _isReadScope ? FileShare.Delete : FileShare.None);

                Log.Debug($"Locked synchronization file '{syncFile}'");
            }
            catch (IOException ex)
            {
                var hResult = (uint)ex.GetHResult();
                if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
                {
                    Log.Warning(ex, $"Failed to lock synchronization file '{syncFile}'");

                    throw Log.ErrorAndCreateException(message => new FileLockScopeException(message, ex), $"Failed to lock synchronization file '{syncFile}'");
                }

                if (_lockAttemptCounter > 0)
                {
                    return false;
                }

                var processes = FileLockInfo.GetProcessesLockingFile(syncFile);
                if (processes is null || !processes.Any())
                {
                    Log.Debug(ex, $"First attempt to lock synchronization file '{syncFile}' was unsuccessful. " +
                                  "Possibly locked by unknown application. Will keep retrying in the background.");
                }
                else
                {
                    Log.Debug($"First attempt to lock synchronization file '{syncFile}' was unsuccessful. " +
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
        var syncFile = _syncFile;
        if (syncFile is null)
        {
            return;
        }

        try
        {
            var fileService = _fileService;
            if (fileService is null)
            {
                return;
            }

            if (fileService.Exists(syncFile))
            {
                fileService.Delete(syncFile);

                Log.Debug($"Deleted synchronization file '{syncFile}'");
            }
        }
        catch (IOException ex)
        {
            var processes = FileLockInfo.GetProcessesLockingFile(syncFile);
            if (processes is null || !processes.Any())
            {
                Log.Warning(ex, $"Failed to delete synchronization file '{syncFile}'");
            }
            else
            {
                Log.Warning(ex, $"Failed to delete synchronization file '{syncFile}' locked by: {string.Join(", ", processes)}");
            }
        }
    }
}
