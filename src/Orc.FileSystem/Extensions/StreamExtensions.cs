namespace Orc.FileSystem;

using System;
using System.IO;
using System.Threading.Tasks;

public static class StreamExtensions
{
    public static byte[] ReadAllBytes(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        const int bufferSize = 2048;

        var bytes = new byte[stream.Length];

        var buffer = new byte[bufferSize];
        var totalBytesRead = 0;
        int bytesRead;

        do
        {
            bytesRead = stream.Read(buffer, 0, bufferSize);

            Buffer.BlockCopy(buffer, 0, bytes, totalBytesRead, bytesRead);

            totalBytesRead += bytesRead;

        } while (bytesRead > 0);

        return bytes;
    }

    public static async Task<byte[]> ReadAllBytesAsync(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        const int bufferSize = 2048;

        var bytes = new byte[stream.Length];

        var buffer = new byte[bufferSize];
        var totalBytesRead = 0;
        var bytesRead = 0;

        do
        {
            bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);

            Buffer.BlockCopy(buffer, 0, bytes, totalBytesRead, bytesRead);

            totalBytesRead += bytesRead;

        } while (bytesRead > 0);

        return bytes;
    }
}
