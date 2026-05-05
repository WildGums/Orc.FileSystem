namespace Orc.FileSystem;

using System;
using System.IO;
using System.Linq;
using Catel;
using Catel.Logging;
using Microsoft.Extensions.Logging;

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    public virtual Stream Create(string fileName)
    {
        Argument.IsNotNullOrWhitespace(() => fileName);

        _logger.LogDebugIfAttached("Creating file '{FileName}'", fileName);

        try
        {
            var stream = File.Create(fileName);
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create file '{FileName}'", fileName);

            throw;
        }
    }

    public virtual Stream Open(string fileName, FileMode fileMode, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.ReadWrite)
    {
        Argument.IsNotNullOrWhitespace(() => fileName);

        _logger.LogDebugIfAttached("Opening file '{FileName}', fileMode: '{FileMode}', fileAccess: '{FileAccess}', fileShare: '{FileShare}'", fileName, fileMode, fileAccess, fileShare);

        try
        {
            var stream = File.Open(fileName, fileMode, fileAccess, fileShare);
            return stream;
        }
        catch (IOException ex)
        {
            var hResult = (uint) ex.GetHResult();

            var message = "Failed to open file '{FileName}'";
            if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
            {
                _logger.LogWarning(ex, message, fileName);

                throw;
            }

            var processes = FileLockInfo.GetProcessesLockingFile(fileName);
            if (processes is null || !processes.Any())
            {                    
                _logger.LogWarning(ex, message, fileName);

                throw;
            }

            _logger.LogWarning("Failed to open file '{FileName}', locked by: {LockedByProcesses}", fileName, string.Join(", ", processes));

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open file '{FileName}'", fileName);

            throw;
        }
    }

    public virtual bool CanOpen(string fileName, FileMode fileMode, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.ReadWrite)
    {
        Argument.IsNotNullOrWhitespace(() => fileName);

        _logger.LogDebugIfAttached("Checking for possibility to open file '{FileName}', fileMode: '{FileMode}', fileAccess: '{FileAccess}', fileShare: '{FileShare}'", fileName, fileMode, fileAccess, fileShare);

        try
        {
            // If file is create => always use append (so we don't change the file)
            var fileMustNotExist = false;
            var fileMustExist = false;
            var finalFileMode = FileMode.Open;

            switch (fileMode)
            {
                case FileMode.CreateNew:
                    finalFileMode = FileMode.Append;
                    fileMustNotExist = true;
                    break;

                case FileMode.Create:
                    finalFileMode = FileMode.Append;
                    break;

                case FileMode.Open:
                    fileMustExist = true;
                    break;

                case FileMode.OpenOrCreate:
                    finalFileMode = FileMode.Append;
                    break;

                case FileMode.Truncate:
                case FileMode.Append:
                    finalFileMode = FileMode.Append;
                    fileMustExist = true;
                    break;

                default:
                    throw _logger.LogErrorAndCreateException(_ => new ArgumentOutOfRangeException(nameof(fileMode), fileMode, null), "Argument out of range");
            }

            if (fileMustExist && !File.Exists(fileName))
            {
                return false;
            }

            if (fileMustNotExist && File.Exists(fileName))
            {
                return false;
            }

            using (File.Open(fileName, finalFileMode, fileAccess, fileShare))
            {
                // Open for test
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public virtual void Copy(string sourceFileName, string destinationFileName, bool overwrite = false)
    {
        Argument.IsNotNullOrWhitespace(() => sourceFileName);
        Argument.IsNotNullOrWhitespace(() => destinationFileName);

        _logger.LogDebugIfAttached("Copying file '{SourceFileName}' => '{DestinationFileName}', overwrite: '{Overwrite}'", sourceFileName, destinationFileName, overwrite);

        try
        {
            File.Copy(sourceFileName, destinationFileName, overwrite);
        }
        catch (IOException ex)
        {
            var hResult = (uint)ex.GetHResult();

            var message = "Failed to copy file '{SourceFileName}' to the '{DestinationFileName}'";

            if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
            {
                _logger.LogWarning(ex, message, sourceFileName, destinationFileName);

                throw;
            }

            var sourceLockingProcesses = FileLockInfo.GetProcessesLockingFile(sourceFileName);
            if (sourceLockingProcesses is not null && sourceLockingProcesses.Any())
            {
                _logger.LogWarning(ex, "Failed to copy file '{SourceFileName}' to the '{DestinationFileName}'\nthe source file is locked by: {SourceLockedByProcesses}", sourceFileName, destinationFileName, string.Join(", ", sourceLockingProcesses));

                throw;
            }

            var destinationLockingProcesses = FileLockInfo.GetProcessesLockingFile(destinationFileName);
            if (destinationLockingProcesses is not null && destinationLockingProcesses.Any())
            {
                _logger.LogWarning(ex, "Failed to copy file '{SourceFileName}' to the '{DestinationFileName}'\nthe destination file is locked by: {DestinationLockedByProcesses}", sourceFileName, destinationFileName, string.Join(", ", destinationLockingProcesses));

                throw;
            }

            _logger.LogWarning(ex, message, sourceFileName, destinationFileName);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to copy file '{SourceFileName}' => '{DestinationFileName}'", sourceFileName, destinationFileName);

            throw;
        }
    }

    public virtual void Move(string sourceFileName, string destinationFileName, bool overwrite = false)
    {
        Argument.IsNotNullOrWhitespace(() => sourceFileName);
        Argument.IsNotNullOrWhitespace(() => destinationFileName);

        _logger.LogDebugIfAttached("Moving file '{SourceFileName}' => '{DestinationFileName}', overwrite: '{Overwrite}'", sourceFileName, destinationFileName, overwrite);

        try
        {
            if (File.Exists(sourceFileName))
            {
                if (File.Exists(destinationFileName) && overwrite)
                {
                    File.Delete(destinationFileName);
                }
            }

            File.Move(sourceFileName, destinationFileName);
        }
        catch (IOException ex)
        {
            var hResult = (uint)ex.GetHResult();

            var message = "Failed to move file '{SourceFileName}' to the '{DestinationFileName}'";

            if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
            {
                _logger.LogWarning(ex, message, sourceFileName, destinationFileName);

                throw;
            }

            var sourceLockingProcesses = FileLockInfo.GetProcessesLockingFile(sourceFileName);
            if (sourceLockingProcesses is not null && sourceLockingProcesses.Any())
            {
                _logger.LogWarning(ex, "Failed to move file '{SourceFileName}' to the '{DestinationFileName}'\nthe source file is locked by: {SourceLockedByProcesses}", sourceFileName, destinationFileName, string.Join(", ", sourceLockingProcesses));

                throw;
            }

            var destinationLockingProcesses = FileLockInfo.GetProcessesLockingFile(destinationFileName);
            if (destinationLockingProcesses is not null && destinationLockingProcesses.Any())
            {
                _logger.LogWarning(ex, "Failed to move file '{SourceFileName}' to the '{DestinationFileName}'\nthe destination file is locked by: {DestinationLockedByProcesses}", sourceFileName, destinationFileName, string.Join(", ", destinationLockingProcesses));

                throw;
            }

            _logger.LogWarning(ex, message, sourceFileName, destinationFileName);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to move file '{SourceFileName}' => '{DestinationFileName}'", sourceFileName, destinationFileName);

            throw;
        }
    }

    public virtual bool Exists(string fileName)
    {
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            var exists = File.Exists(fileName);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check whether file '{FileName}' exists", fileName);

            throw;
        }
    }

    public virtual void Delete(string fileName)
    {
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            if (File.Exists(fileName))
            {
                _logger.LogDebugIfAttached("Deleting file '{FileName}'", fileName);

                File.Delete(fileName);
            }
        }
        catch (IOException ex)
        {
            var hResult = (uint)ex.GetHResult();

            var message = "Failed to delete file '{FileName}'";
            if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
            {
                _logger.LogWarning(ex, message, fileName);

                throw;
            }

            var processes = FileLockInfo.GetProcessesLockingFile(fileName);
            if (processes is null || !processes.Any())
            {
                _logger.LogWarning(ex, message, fileName);

                throw;
            }

            _logger.LogWarning("Failed to delete file '{FileName}', locked by: {LockedByProcesses}", fileName, string.Join(", ", processes));

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file '{FileName}'", fileName);

            throw;
        }
    }
}
