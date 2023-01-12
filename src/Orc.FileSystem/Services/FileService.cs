namespace Orc.FileSystem
{
    using System;
    using System.IO;
    using System.Linq;
    using Catel;
    using Catel.Logging;

    public class FileService : IFileService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public FileStream Create(string fileName)
        {
            Argument.IsNotNullOrWhitespace(() => fileName);

            Log.DebugIfAttached($"Creating file '{fileName}'");

            try
            {
                var fileStream = File.Create(fileName);
                return fileStream;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to create file '{fileName}'");

                throw;
            }
        }

        public FileStream Open(string fileName, FileMode fileMode, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.ReadWrite)
        {
            Argument.IsNotNullOrWhitespace(() => fileName);

            Log.DebugIfAttached($"Opening file '{fileName}', fileMode: '{fileMode}', fileAccess: '{fileAccess}', fileShare: '{fileShare}'");

            try
            {
                var fileStream = File.Open(fileName, fileMode, fileAccess, fileShare);
                return fileStream;
            }
            catch (IOException ex)
            {
                var hResult = (uint) ex.GetHResult();

                var message = $"Failed to open file '{fileName}'";
                if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
                {
                    Log.Warning(ex, message);

                    throw;
                }

                var processes = FileLockInfo.GetProcessesLockingFile(fileName);
                if (processes is null || !processes.Any())
                {                    
                    Log.Warning(ex, message);

                    throw;
                }

                Log.Warning(message + $", locked by: {string.Join(", ", processes)}");

                throw;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to open file '{fileName}'");

                throw;
            }
        }

        public bool CanOpen(string fileName, FileMode fileMode, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.ReadWrite)
        {
            Argument.IsNotNullOrWhitespace(() => fileName);

            Log.DebugIfAttached($"Checking for possibility to open file '{fileName}', fileMode: '{fileMode}', fileAccess: '{fileAccess}', fileShare: '{fileShare}'");

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
                        throw Log.ErrorAndCreateException(_ => new ArgumentOutOfRangeException(nameof(fileMode), fileMode, null), "Argument out of range");
                }

                if (fileMustExist && !File.Exists(fileName))
                {
                    return false;
                }

                if (fileMustNotExist && File.Exists(fileName))
                {
                    return false;
                }

                using (var fileStream = File.Open(fileName, finalFileMode, fileAccess, fileShare))
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

        public void Copy(string sourceFileName, string destinationFileName, bool overwrite = false)
        {
            Argument.IsNotNullOrWhitespace(() => sourceFileName);
            Argument.IsNotNullOrWhitespace(() => destinationFileName);

            Log.DebugIfAttached($"Copying file '{sourceFileName}' => '{destinationFileName}', overwrite: '{overwrite}'");

            try
            {
                File.Copy(sourceFileName, destinationFileName, overwrite);
            }
            catch (IOException ex)
            {
                var hResult = (uint)ex.GetHResult();

                var message = $"Failed to copy file '{sourceFileName}' to the '{destinationFileName}'";

                if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
                {
                    Log.Warning(ex, message);

                    throw;
                }

                var sourceLockingProcesses = FileLockInfo.GetProcessesLockingFile(sourceFileName);
                if (sourceLockingProcesses is not null && sourceLockingProcesses.Any())
                {
                    Log.Warning(ex, message + $"\nthe file file '{sourceFileName}', locked by: {string.Join(", ", sourceLockingProcesses)}");

                    throw;
                }

                var destinationLockingProcesses = FileLockInfo.GetProcessesLockingFile(destinationFileName);
                if (destinationLockingProcesses is not null && destinationLockingProcesses.Any())
                {
                    Log.Warning(ex, message + $"\nthe file '{destinationFileName}', locked by: {string.Join(", ", destinationLockingProcesses)}");

                    throw;
                }

                Log.Warning(ex, message);

                throw;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to copy file '{sourceFileName}' => '{destinationFileName}'");

                throw;
            }
        }

        public void Move(string sourceFileName, string destinationFileName, bool overwrite = false)
        {
            Argument.IsNotNullOrWhitespace(() => sourceFileName);
            Argument.IsNotNullOrWhitespace(() => destinationFileName);

            Log.DebugIfAttached($"Moving file '{sourceFileName}' => '{destinationFileName}', overwrite: '{overwrite}'");

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

                var message = $"Failed to move file '{sourceFileName}' to the '{destinationFileName}'";

                if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
                {
                    Log.Warning(ex, message);

                    throw;
                }

                var sourceLockingProcesses = FileLockInfo.GetProcessesLockingFile(sourceFileName);
                if (sourceLockingProcesses is not null && sourceLockingProcesses.Any())
                {
                    Log.Warning(ex, message + $"\nthe file file '{sourceFileName}', locked by: {string.Join(", ", sourceLockingProcesses)}");

                    throw;
                }

                var destinationLockingProcesses = FileLockInfo.GetProcessesLockingFile(destinationFileName);
                if (destinationLockingProcesses is not null && destinationLockingProcesses.Any())
                {
                    Log.Warning(ex, message + $"\nthe file '{destinationFileName}', locked by: {string.Join(", ", destinationLockingProcesses)}");

                    throw;
                }

                Log.Warning(ex, message);

                throw;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to move file '{sourceFileName}' => '{destinationFileName}'");

                throw;
            }
        }

        public bool Exists(string fileName)
        {
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                var exists = File.Exists(fileName);
                return exists;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to check whether file '{fileName}' exists");

                throw;
            }
        }

        public void Delete(string fileName)
        {
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                if (File.Exists(fileName))
                {
                    Log.DebugIfAttached($"Deleting file '{fileName}'");

                    File.Delete(fileName);
                }
            }
            catch (IOException ex)
            {
                var hResult = (uint)ex.GetHResult();

                var message = $"Failed to delete file '{fileName}'";
                if (hResult != SystemErrorCodes.ERROR_SHARING_VIOLATION)
                {
                    Log.Warning(ex, message);

                    throw;
                }

                var processes = FileLockInfo.GetProcessesLockingFile(fileName);
                if (processes is null || !processes.Any())
                {
                    Log.Warning(ex, message);

                    throw;
                }

                Log.Warning(message + $", locked by: {string.Join(", ", processes)}");

                throw;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to delete file '{fileName}'");

                throw;
            }
        }
    }
}
