namespace Orc.FileSystem.Tests;

using System;
using System.IO;

public sealed class TemporaryFilesContext : IDisposable
{
    private readonly Guid _randomGuid = Guid.NewGuid();
    private readonly string _rootDirectory;

    public TemporaryFilesContext(string name = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = _randomGuid.ToString();
        }

        _rootDirectory = Path.Combine(Path.GetTempPath(), GetType().Assembly.GetName().Name, name);

        Directory.CreateDirectory(_rootDirectory);
    }

    public void Dispose()
    {
        //Logger.LogDebug("Deleting temporary files from '{0}'", _rootDirectory);

        try
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, true);
            }
        }
        catch (Exception)
        {
            //Logger.LogWarning(ex, "Failed to delete temporary files");
        }
    }

    public string GetDirectory(string relativeDirectoryName)
    {
        var fullPath = Path.Combine(_rootDirectory, relativeDirectoryName);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        return fullPath;
    }

    public string GetFile(string relativeFilePath, bool deleteIfExists = false)
    {
        var fullPath = Path.Combine(_rootDirectory, relativeFilePath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (deleteIfExists)
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        return fullPath;
    }
}
