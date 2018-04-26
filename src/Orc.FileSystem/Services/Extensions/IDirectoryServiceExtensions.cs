// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDirectoryServiceExtensions.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.IO;
    using System.Linq;
    using Catel;
    using Catel.Logging;

    public static class IDirectoryServiceExtensions
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public static bool IsEmpty(this IDirectoryService directoryService, string path)
        {
            Argument.IsNotNull(() => directoryService);
            Argument.IsNotNull(() => path);

            if (!directoryService.Exists(path))
            {
                // If it doesn't exist, it's empty
                return true;
            }

            if (directoryService.GetFiles(path).Any())
            {
                return false;
            }

            // We are assuming that, even if we have subdirectories, they could all be empty (e.g. we are checking for
            // an empty directory tree)
            foreach (var subDirectory in directoryService.GetDirectories(path))
            {
                if (!IsEmpty(directoryService, subDirectory))
                {
                    return false;
                }
            }

            return true;
        }

        public static ulong GetSize(this IDirectoryService directoryService, string path)
        {
            Argument.IsNotNull(() => directoryService);
            Argument.IsNotNull(() => path);

            ulong size = 0L;

            try
            {
                if (directoryService.Exists(path))
                {
                    size += (ulong)(from fileName in directoryService.GetFiles(path, "*", SearchOption.AllDirectories)
                                    select new FileInfo(fileName)).Sum(x => x.Length);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to calculate the size of directory '{0}'", path);
            }

            return size;
        }
    }
}