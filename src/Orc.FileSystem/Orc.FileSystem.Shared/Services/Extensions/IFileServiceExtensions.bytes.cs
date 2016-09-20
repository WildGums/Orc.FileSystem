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
        public static byte[] ReadAllBytes(this IFileService fileService, string fileName)
        {
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    Log.Debug($"Reading all bytes from '{fileName}'");

                    var bytes = stream.ReadAllBytes();
                    return bytes;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to read all bytes from '{fileName}'");

                throw;
            }
        }

        public static async Task<byte[]> ReadAllBytesAsync(this IFileService fileService, string fileName)
        {
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    Log.Debug($"Reading all bytes from '{fileName}'");

                    var bytes = await stream.ReadAllBytesAsync();
                    return bytes;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to read all bytes from '{fileName}'");

                throw;
            }
        }

        public static void WriteAllBytes(this IFileService fileService, string fileName, byte[] bytes)
        {
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write))
                {
                    Log.Debug($"Writing '{bytes.Length}' bytes to '{fileName}'");

                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to write '{bytes.Length}' bytes to '{fileName}'");

                throw;
            }
        }

        public static async Task WriteAllBytesAsync(this IFileService fileService, string fileName, byte[] bytes)
        {
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                using (var stream = fileService.Open(fileName, FileMode.Create, FileAccess.Write))
                {
                    Log.Debug($"Writing '{bytes.Length}' bytes to '{fileName}'");

                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to write '{bytes.Length}' bytes to '{fileName}'");

                throw;
            }
        }
    }
}