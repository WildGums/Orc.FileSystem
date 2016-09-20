// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DirectoryService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System.IO;
    using Catel;
    using Catel.Logging;

    public class DirectoryService : IDirectoryService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public string Create(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            if (!Directory.Exists(path))
            {
                Log.Info($"Creating directory '{path}'");

                var info = Directory.CreateDirectory(path);
            }

            return path;
        }

        public void Move(string sourcePath, string destinationPath)
        {
            Argument.IsNotNullOrWhitespace(() => sourcePath);
            Argument.IsNotNullOrWhitespace(() => destinationPath);

            Log.Info($"Moving directory '{sourcePath}' => '{destinationPath}'");

            Directory.Move(sourcePath, destinationPath);
        }

        public void Delete(string path, bool recursive)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            if (Directory.Exists(path))
            {
                Log.Info($"Deleting directory '{path}', recursive: '{recursive}'");

                Directory.Delete(path, recursive);
            }
        }

        public bool Exists(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            var exists = Directory.Exists(path);
            return exists;
        }

        public string[] GetDirectories(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            Log.Debug($"Getting directories inside '{path}', searchPattern: '{searchPattern}', searchOption: '{searchOption}'");

            var directories = Directory.GetDirectories(path, searchPattern, searchOption);
            return directories;
        }

        public string[] GetFiles(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            Log.Debug($"Getting files inside '{path}', searchPattern: '{searchPattern}', searchOption: '{searchOption}'");

            var files = Directory.GetFiles(path, searchPattern, searchOption);
            return files;
        }
    }
}