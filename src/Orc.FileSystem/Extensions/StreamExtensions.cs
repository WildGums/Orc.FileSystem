// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensions.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Catel;

    public static class StreamExtensions
    {
        public static byte[] ReadAllBytes(this Stream stream)
        {
            Argument.IsNotNull(() => stream);

            const int BufferSize = 2048;

            var bytes = new byte[stream.Length];

            var buffer = new byte[BufferSize];
            var totalBytesRead = 0;
            var bytesRead = 0;

            do
            {
                bytesRead = stream.Read(buffer, 0, BufferSize);

                Buffer.BlockCopy(buffer, 0, bytes, totalBytesRead, bytesRead);

                totalBytesRead += bytesRead;

            } while (bytesRead > 0);

            return bytes;
        }

        public static async Task<byte[]> ReadAllBytesAsync(this Stream stream)
        {
            Argument.IsNotNull(() => stream);

            const int BufferSize = 2048;

            var bytes = new byte[stream.Length];

            var buffer = new byte[BufferSize];
            var totalBytesRead = 0;
            var bytesRead = 0;

            do
            {
                bytesRead = await stream.ReadAsync(buffer, 0, BufferSize);

                Buffer.BlockCopy(buffer, 0, bytes, totalBytesRead, bytesRead);

                totalBytesRead += bytesRead;

            } while (bytesRead > 0);

            return bytes;
        }
    }
}