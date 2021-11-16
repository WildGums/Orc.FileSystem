// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFileServiceExtensions.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
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
                using (var stream = fileService.OpenRead(fileName))
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
                Log.Warning(ex, $"Failed to read all lines from '{fileName}'");

                throw;
            }
        }

        public static async Task<string[]> ReadAllLinesAsync(this IFileService fileService, string fileName)
        {
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.OpenRead(fileName))
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
                Log.Warning(ex, $"Failed to read all lines from '{fileName}'");

                throw;
            }
        }

        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.1")]
        public static void WriteAllLines(this IFileService fileService, string fileName, string[] lines)
        {
            WriteAllLines(fileService, fileName, (IEnumerable<string>)lines);
        }

        public static Task WriteAllLinesAsync(this IFileService fileService, string fileName, string[] lines)
        {
            return WriteAllLinesAsync(fileService, fileName, (IEnumerable<string>)lines);
        }

        public static void WriteAllLines(this IFileService fileService, string fileName, IEnumerable<string> lines)
        {
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            var count = 0;

            try
            {
                count = lines.Count();

                using (var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Log.Debug($"Writing '{count}' lines to '{fileName}'");

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
                Log.Warning(ex, $"Failed to write '{count}' lines to '{fileName}'");

                throw;
            }
        }

        public static async Task WriteAllLinesAsync(this IFileService fileService, string fileName, IEnumerable<string> lines)
        {
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            var count = 0;

            try
            {
                count = lines.Count();

                using (var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Log.Debug($"Writing '{count}' lines to '{fileName}'");

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
                Log.Warning(ex, $"Failed to write '{count}' lines to '{fileName}'");

                throw;
            }
        }
    }
}
