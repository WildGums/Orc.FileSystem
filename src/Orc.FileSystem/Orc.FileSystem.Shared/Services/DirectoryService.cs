// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DirectoryService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.IO;
    using Catel;
    using Catel.Logging;

    public class DirectoryService : IDirectoryService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public string Create(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                if (!Directory.Exists(path))
                {
                    Log.Debug($"Creating directory '{path}'");

                    var info = Directory.CreateDirectory(path);
                    path = info.FullName;
                }

                return path;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to create directory '{path}'");

                throw;
            }
        }

        public void Move(string sourcePath, string destinationPath)
        {
            Argument.IsNotNullOrWhitespace(() => sourcePath);
            Argument.IsNotNullOrWhitespace(() => destinationPath);

            try
            {
                Log.Debug($"Moving directory '{sourcePath}' => '{destinationPath}'");

                Directory.Move(sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to move directory '{sourcePath}' => '{destinationPath}'");

                throw;
            }
        }

        public void Delete(string path, bool recursive)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                if (Directory.Exists(path))
                {
                    Log.Debug($"Deleting directory '{path}', recursive: '{recursive}'");

                    Directory.Delete(path, recursive);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to delete directory '{path}'");

                throw;
            }
        }

        public bool Exists(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                var exists = Directory.Exists(path);
                return exists;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to check whether directory '{path}' exists");

                throw;
            }
        }

        public string[] GetDirectories(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                Log.Debug($"Getting directories inside '{path}', searchPattern: '{searchPattern}', searchOption: '{searchOption}'");

                var directories = Directory.GetDirectories(path, searchPattern, searchOption);
                return directories;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed getting directories inside '{path}'");

                throw;
            }
        }

        public string[] GetFiles(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {

                Log.Debug($"Getting files inside '{path}', searchPattern: '{searchPattern}', searchOption: '{searchOption}'");

                var files = Directory.GetFiles(path, searchPattern, searchOption);
                return files;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed getting files inside '{path}'");

                throw;
            }
        }
    }
}