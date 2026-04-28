namespace Orc.FileSystem;

using System;
using System.IO;
using System.Linq;
using Catel.Logging;
using Microsoft.Extensions.Logging;

public static class IDirectoryServiceExtensions
{
    private static readonly ILogger Logger = LogManager.GetLogger(typeof(IDirectoryServiceExtensions));

    public static bool IsEmpty(this IDirectoryService directoryService, string path)
    {
        ArgumentNullException.ThrowIfNull(directoryService);
        ArgumentNullException.ThrowIfNull(path);

        if (!directoryService.Exists(path))
        {
            // If it doesn't exist, it's empty
            return true;
        }

        if (directoryService.GetFiles(path).Any())
        {
            return false;
        }

        // We are assuming that, even if we have subdirectories, they could all be empty (e.g. we are checking for
        // an empty directory tree)
        foreach (var subDirectory in directoryService.GetDirectories(path))
        {
            if (!IsEmpty(directoryService, subDirectory))
            {
                return false;
            }
        }

        return true;
    }

    public static ulong GetSize(this IDirectoryService directoryService, string path)
    {
        ArgumentNullException.ThrowIfNull(directoryService);
        ArgumentNullException.ThrowIfNull(path);

        ulong size = 0L;

        try
        {
            if (directoryService.Exists(path))
            {
                size += (ulong)(from fileName in directoryService.GetFiles(path, "*", SearchOption.AllDirectories)
                    select new FileInfo(fileName)).Sum(x => x.Length);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to calculate the size of directory '{0}'", path);
        }

        return size;
    }
}
