namespace Orc.FileSystem
{
    using System;
    using System.IO;
    using Catel.Logging;

    public static partial class IFileServiceExtensions
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public static bool CanOpenRead(this IFileService fileService, string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileService);

            return fileService.CanOpen(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public static Stream OpenRead(this IFileService fileService, string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileService);

            return fileService.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public static bool CanOpenWrite(this IFileService fileService, string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileService);

            return fileService.CanOpen(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        public static Stream OpenWrite(this IFileService fileService, string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileService);

            return fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        }
    }
}
