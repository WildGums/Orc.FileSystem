namespace Orc.FileSystem;

using System;
using System.IO;
using Catel;
using Catel.Logging;

public class DirectoryService : IDirectoryService
{
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

    private readonly IFileService _fileService;

    public DirectoryService(IFileService fileService)
    {
        ArgumentNullException.ThrowIfNull(fileService);

        _fileService = fileService;
    }

    public string Create(string path)
    {
        Argument.IsNotNullOrWhitespace(() => path);

        try
        {
            if (Directory.Exists(path))
            {
                return path;
            }

            Log.Debug($"Creating directory '{path}'");

            var info = Directory.CreateDirectory(path);
            path = info.FullName;

            return path;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Failed to create directory '{path}'");

            throw;
        }
    }

    public void Move(string sourcePath, string destinationPath)
    {
        Argument.IsNotNullOrWhitespace(() => sourcePath);
        Argument.IsNotNullOrWhitespace(() => destinationPath);

        try
        {
            Log.Debug($"Moving directory '{sourcePath}' => '{destinationPath}'");

            Directory.Move(sourcePath, destinationPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Failed to move directory '{sourcePath}' => '{destinationPath}'");

            throw;
        }
    }

    public void Copy(string sourcePath, string destinationPath, bool copySubDirs = true, bool overwriteExisting = false)
    {
        Argument.IsNotNullOrWhitespace(() => sourcePath);
        Argument.IsNotNullOrWhitespace(() => destinationPath);

        if (!Exists(sourcePath))
        {
            Log.Warning($"Source directory '{sourcePath}' does not exist or could not be found");

            throw Log.ErrorAndCreateException<DirectoryNotFoundException>($"Source directory '{sourcePath}' does not exist or could not be found");
        }

        Log.Debug($"Copying directory '{sourcePath}' to '{destinationPath}'");

        Create(destinationPath);

        var files = GetFiles(sourcePath);
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var destinationFileName = Path.Combine(destinationPath, fileName);

            if (File.Exists(destinationFileName) && !overwriteExisting)
            {
                Log.Debug($"Skipping copying of '{file}', file already exists in target directory");
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

    public void Delete(string path, bool recursive = true)
    {
        Argument.IsNotNullOrWhitespace(() => path);

        try
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            Log.Debug($"Deleting directory '{path}', recursive: '{recursive}'");

            Directory.Delete(path, recursive);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Failed to delete directory '{path}'");

            throw;
        }
    }

    public bool Exists(string path)
    {
        Argument.IsNotNullOrWhitespace(() => path);

        try
        {
            var exists = Directory.Exists(path);
            return exists;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Failed to check whether directory '{path}' exists");

            throw;
        }
    }

    public string[] GetDirectories(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        Argument.IsNotNullOrWhitespace(() => path);

        try
        {
            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                searchPattern = "*";
            }

            Log.Debug($"Getting directories inside '{path}', searchPattern: '{searchPattern}', searchOption: '{searchOption}'");

            var directories = Directory.GetDirectories(path, searchPattern, searchOption);
            return directories;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Failed getting directories inside '{path}'");

            throw;
        }
    }

    public string[] GetFiles(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        Argument.IsNotNullOrWhitespace(() => path);

        try
        {
            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                searchPattern = "*";
            }

            Log.Debug($"Getting files inside '{path}', searchPattern: '{searchPattern}', searchOption: '{searchOption}'");

            var files = Directory.GetFiles(path, searchPattern, searchOption);
            return files;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Failed getting files inside '{path}'");

            throw;
        }
    }
}
