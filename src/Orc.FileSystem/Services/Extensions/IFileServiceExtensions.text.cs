namespace Orc.FileSystem
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Catel;
    using Catel.Logging;

    public static partial class IFileServiceExtensions
    {
        public static string ReadAllText(this IFileService fileService, string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.OpenRead(fileName))
                {
                    Log.Debug($"Reading all text from '{fileName}'");

                    using (var reader = new StreamReader(stream))
                    {
                        var text = reader.ReadToEnd();
                        return text;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to read all text from '{fileName}'");

                throw;
            }
        }

        public static async Task<string> ReadAllTextAsync(this IFileService fileService, string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.OpenRead(fileName))
                {
                    Log.Debug($"Reading all text from '{fileName}'");

                    using (var reader = new StreamReader(stream))
                    {
                        var text = await reader.ReadToEndAsync();
                        return text;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to read all text from '{fileName}'");

                throw;
            }
        }

        public static void WriteAllText(this IFileService fileService, string fileName, string text)
        {
            ArgumentNullException.ThrowIfNull(fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Log.Debug($"Writing text to '{fileName}'");

                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(text);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to write text to '{fileName}'");

                throw;
            }
        }

        public static async Task WriteAllTextAsync(this IFileService fileService, string fileName, string text)
        {
            ArgumentNullException.ThrowIfNull(fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Log.Debug($"Writing text to '{fileName}'");

                    using (var writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(text);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to write text to '{fileName}'");

                throw;
            }
        }
    }
}
