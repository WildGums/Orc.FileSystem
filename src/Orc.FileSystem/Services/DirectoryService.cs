namespace Orc.FileSystem;

using System;
using System.IO;
using Catel;
using Catel.Logging;
using Microsoft.Extensions.Logging;

public class DirectoryService : IDirectoryService
{
    private readonly ILogger<DirectoryService> _logger;
    private readonly IFileService _fileService;

    public DirectoryService(ILogger<DirectoryService> logger, IFileService fileService)
    {
        _logger = logger;
        _fileService = fileService;
    }

    public virtual string Create(string path)
    {
        Argument.IsNotNullOrWhitespace(() => path);

        try
        {
            if (Directory.Exists(path))
            {
                return path;
            }

            _logger.LogDebug($"Creating directory '{path}'");

            var info = Directory.CreateDirectory(path);
            path = info.FullName;

            return path;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to create directory '{path}'");

            throw;
        }
    }

    public virtual void Move(string sourcePath, string destinationPath)
    {
        Argument.IsNotNullOrWhitespace(() => sourcePath);
        Argument.IsNotNullOrWhitespace(() => destinationPath);

        try
        {
            _logger.LogDebug($"Moving directory '{sourcePath}' => '{destinationPath}'");

            Directory.Move(sourcePath, destinationPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to move directory '{sourcePath}' => '{destinationPath}'");

            throw;
        }
    }

    public virtual void Copy(string sourcePath, string destinationPath, bool copySubDirs = true, bool overwriteExisting = false)
    {
        Argument.IsNotNullOrWhitespace(() => sourcePath);
        Argument.IsNotNullOrWhitespace(() => destinationPath);

        if (!Exists(sourcePath))
        {
            _logger.LogWarning($"Source directory '{sourcePath}' does not exist or could not be found");

            throw _logger.LogErrorAndCreateException<DirectoryNotFoundException>($"Source directory '{sourcePath}' does not exist or could not be found");
        }

        _logger.LogDebug($"Copying directory '{sourcePath}' to '{destinationPath}'");

        Create(destinationPath);

        var files = GetFiles(sourcePath);
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var destinationFileName = Path.Combine(destinationPath, fileName);

            if (File.Exists(destinationFileName) && !overwriteExisting)
            {
                _logger.LogDebug($"Skipping copying of '{file}', file already exists in target directory");
                continue;
            }

            _fileService.Copy(file, destinationFileName, overwriteExisting);
        }

        if (!copySubDirs)
        {
            return;
        }

        var subDirectories = GetDirectories(sourcePath);

        foreach (var subDirectory in subDirectories)
        {
            var subDirectoryName = Path.GetDirectoryName(subDirectory);
            var destinationSubDirectory = Path.Combine(destinationPath, subDirectoryName ?? string.Empty);

            Copy(subDirectory, destinationSubDirectory, copySubDirs, overwriteExisting);
        }
    }

    public virtual void Delete(string path, bool recursive = true)
    {
        Argument.IsNotNullOrWhitespace(() => path);

        try
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            _logger.LogDebug($"Deleting directory '{path}', recursive: '{recursive}'");

            Directory.Delete(path, recursive);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to delete directory '{path}'");

            throw;
        }
    }

    public virtual bool Exists(string path)
    {
        Argument.IsNotNullOrWhitespace(() => path);

        try
        {
            var exists = Directory.Exists(path);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to check whether directory '{path}' exists");

            throw;
        }
    }

    public virtual string[] GetDirectories(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        Argument.IsNotNullOrWhitespace(() => path);

        try
        {
            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                searchPattern = "*";
            }

            _logger.LogDebug($"Getting directories inside '{path}', searchPattern: '{searchPattern}', searchOption: '{searchOption}'");

            var directories = Directory.GetDirectories(path, searchPattern, searchOption);
            return directories;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed getting directories inside '{path}'");

            throw;
        }
    }

    public virtual string[] GetFiles(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        Argument.IsNotNullOrWhitespace(() => path);

        try
        {
            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                searchPattern = "*";
            }

            _logger.LogDebug($"Getting files inside '{path}', searchPattern: '{searchPattern}', searchOption: '{searchOption}'");

            var files = Directory.GetFiles(path, searchPattern, searchOption);
            return files;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed getting files inside '{path}'");

            throw;
        }
    }
}
