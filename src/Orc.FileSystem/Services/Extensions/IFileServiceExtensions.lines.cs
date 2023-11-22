namespace Orc.FileSystem;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Catel;
using Catel.Logging;

public static partial class IFileServiceExtensions
{
    public static string[] ReadAllLines(this IFileService fileService, string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            using var stream = fileService.OpenRead(fileName);
            Log.Debug($"Reading all lines from '{fileName}'");

            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();

            var lines = text.Split(new [] { Environment.NewLine }, StringSplitOptions.None);
            return lines;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Failed to read all lines from '{fileName}'");

            throw;
        }
    }

    public static async Task<string[]> ReadAllLinesAsync(this IFileService fileService, string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        try
        {
            await using var stream = fileService.OpenRead(fileName);
            Log.Debug($"Reading all lines from '{fileName}'");

            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync();

            var lines = text.Split(new [] { Environment.NewLine }, StringSplitOptions.None);
            return lines;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Failed to read all lines from '{fileName}'");

            throw;
        }
    }

    public static Task WriteAllLinesAsync(this IFileService fileService, string fileName, string[] lines)
    {
        return WriteAllLinesAsync(fileService, fileName, (IEnumerable<string>)lines);
    }

    public static void WriteAllLines(this IFileService fileService, string fileName, IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        var count = 0;

        try
        {
            count = lines.Count();

            using var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            Log.Debug($"Writing '{count}' lines to '{fileName}'");

            using var writer = new StreamWriter(stream);
            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Failed to write '{count}' lines to '{fileName}'");

            throw;
        }
    }

    public static async Task WriteAllLinesAsync(this IFileService fileService, string fileName, IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        Argument.IsNotNullOrWhitespace(() => fileName);

        var count = 0;

        try
        {
            count = lines.Count();

            await using var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            Log.Debug($"Writing '{count}' lines to '{fileName}'");

            await using var writer = new StreamWriter(stream);
            foreach (var line in lines)
            {
                await writer.WriteLineAsync(line);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Failed to write '{count}' lines to '{fileName}'");

            throw;
        }
    }
}
