// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFileServiceExtensions.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Catel;
    using Catel.Logging;

    public static partial class IFileServiceExtensions
    {
        public static string[] ReadAllLines(this IFileService fileService, string fileName)
        {
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Log.Debug($"Reading all lines from '{fileName}'");

                    using (var reader = new StreamReader(stream))
                    {
                        var text = reader.ReadToEnd();

                        var lines = text.Split(new [] { Environment.NewLine }, StringSplitOptions.None);
                        return lines;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to read all lines from '{fileName}'");

                throw;
            }
        }

        public static async Task<string[]> ReadAllLinesAsync(this IFileService fileService, string fileName)
        {
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Log.Debug($"Reading all lines from '{fileName}'");

                    using (var reader = new StreamReader(stream))
                    {
                        var text = await reader.ReadToEndAsync();

                        var lines = text.Split(new [] { Environment.NewLine }, StringSplitOptions.None);
                        return lines;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to read all lines from '{fileName}'");

                throw;
            }
        }

        public static void WriteAllLines(this IFileService fileService, string fileName, string[] lines)
        {
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Log.Debug($"Writing '{lines.Length}' lines to '{fileName}'");

                    using (var writer = new StreamWriter(stream))
                    {
                        foreach (var line in lines)
                        {
                            writer.WriteLine(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to write '{lines.Length}' lines to '{fileName}'");

                throw;
            }
        }

        public static async Task WriteAllLinesAsync(this IFileService fileService, string fileName, string[] lines)
        {
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Log.Debug($"Writing '{lines.Length}' lines to '{fileName}'");

                    using (var writer = new StreamWriter(stream))
                    {
                        foreach (var line in lines)
                        {
                            await writer.WriteLineAsync(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to write '{lines.Length}' lines to '{fileName}'");

                throw;
            }
        }
    }
}