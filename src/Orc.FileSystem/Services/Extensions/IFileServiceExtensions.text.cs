namespace Orc.FileSystem;

using System;
using System.IO;
using System.Threading.Tasks;
using Catel;
using Catel.Logging;
using Microsoft.Extensions.Logging;

public static partial class IFileServiceExtensions
{
    public static string ReadAllText(this IFileService fileService, string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            using var stream = fileService.OpenRead(fileName);
            Logger.LogDebug("Reading all text from '{FileName}'", fileName);

            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();
            return text;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to read all text from '{FileName}'", fileName);

            throw;
        }
    }

    public static async Task<string> ReadAllTextAsync(this IFileService fileService, string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            await using var stream = fileService.OpenRead(fileName);
            Logger.LogDebug("Reading all text from '{FileName}'", fileName);

            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync();
            return text;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to read all text from '{FileName}'", fileName);

            throw;
        }
    }

    public static void WriteAllText(this IFileService fileService, string fileName, string text)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            using var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            Logger.LogDebug("Writing text to '{FileName}'", fileName);

            using var writer = new StreamWriter(stream);
            writer.Write(text);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to write text to '{FileName}'", fileName);

            throw;
        }
    }

    public static async Task WriteAllTextAsync(this IFileService fileService, string fileName, string text)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            await using var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            Logger.LogDebug("Writing text to '{FileName}'", fileName);

            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(text);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to write text to '{FileName}'", fileName);

            throw;
        }
    }
}
