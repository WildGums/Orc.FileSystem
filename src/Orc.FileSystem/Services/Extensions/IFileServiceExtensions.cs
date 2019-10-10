// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFileServiceExtensions.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System.IO;
    using Catel;
    using Catel.Logging;

    public static partial class IFileServiceExtensions
    {
        #region Constants
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        #endregion

        public static bool CanOpenRead(this IFileService fileService, string fileName)
        {
            Argument.IsNotNull(() => fileService);

            return fileService.CanOpen(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public static FileStream OpenRead(this IFileService fileService, string fileName)
        {
            Argument.IsNotNull(() => fileService);

            return fileService.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public static bool CanOpenWrite(this IFileService fileService, string fileName)
        {
            Argument.IsNotNull(() => fileService);

            return fileService.CanOpen(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        public static FileStream OpenWrite(this IFileService fileService, string fileName)
        {
            Argument.IsNotNull(() => fileService);

            return fileService.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        }
    }
}
