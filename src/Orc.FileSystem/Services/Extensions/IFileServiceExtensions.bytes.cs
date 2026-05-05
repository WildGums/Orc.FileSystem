namespace Orc.FileSystem;

using System;
using System.IO;
using System.Threading.Tasks;
using Catel;
using Catel.Logging;
using Microsoft.Extensions.Logging;

public static partial class IFileServiceExtensions
{
    public static byte[] ReadAllBytes(this IFileService fileService, string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            using var stream = fileService.OpenRead(fileName);
            Logger.LogDebug("Reading all bytes from '{FileName}'", fileName);

            var bytes = stream.ReadAllBytes();
            return bytes;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to read all bytes from '{FileName}'", fileName);

            throw;
        }
    }

    public static async Task<byte[]> ReadAllBytesAsync(this IFileService fileService, string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            await using var stream = fileService.OpenRead(fileName);
            Logger.LogDebug("Reading all bytes from '{FileName}'", fileName);

            var bytes = await stream.ReadAllBytesAsync();
            return bytes;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to read all bytes from '{FileName}'", fileName);

            throw;
        }
    }

    public static void WriteAllBytes(this IFileService fileService, string fileName, byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            using var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            Logger.LogDebug("Writing '{ByteCount}' bytes to '{FileName}'", bytes.Length, fileName);

            stream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to write '{ByteCount}' bytes to '{FileName}'", bytes.Length, fileName);

            throw;
        }
    }

    public static async Task WriteAllBytesAsync(this IFileService fileService, string fileName, byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            await using var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            Logger.LogDebug("Writing '{ByteCount}' bytes to '{FileName}'", bytes.Length, fileName);

            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to write '{ByteCount}' bytes to '{FileName}'", bytes.Length, fileName);

            throw;
        }
    }
}
