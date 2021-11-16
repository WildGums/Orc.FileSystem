// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DirectoryService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using Catel;
    using Catel.IoC;
    using Catel.Logging;

    public class DirectoryService : IDirectoryService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IFile _file;
        private readonly IDirectory _directory;
        private readonly IPath _path;
        public DirectoryService(IFileSystem fileSystem)
        {
            Argument.IsNotNull(() => fileSystem);

            _file = fileSystem.File;
            _directory = fileSystem.Directory;
            _path = fileSystem.Path;
        }
        
        [ObsoleteEx]
        public DirectoryService(IFileService fileService)
        {
            Argument.IsNotNull(() => fileService);

            var serviceLocator = fileService.GetServiceLocator();
            _file = serviceLocator.ResolveType<IFileSystem>().File;
        }

        public string Create(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                if (!_directory.Exists(path))
                {
                    Log.Debug($"Creating directory '{path}'");

                    var info = _directory.CreateDirectory(path);
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

                _directory.Move(sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to move directory '{sourcePath}' => '{destinationPath}'");

                throw;
            }
        }

        public void Copy(string sourcePath, string destinationPath, bool copySubDirs = true, bool overwriteExisting = false)
        {
            Argument.IsNotNullOrWhitespace(() => sourcePath);
            Argument.IsNotNullOrWhitespace(() => destinationPath);

            if (!Exists(sourcePath))
            {
                throw Log.ErrorAndCreateException<DirectoryNotFoundException>($"Source directory '{sourcePath}' does not exist or could not be found");
            }

            Log.Debug($"Copying directory '{sourcePath}' to '{destinationPath}'");

            Create(destinationPath);

            var files = GetFiles(sourcePath);
            foreach (var file in files)
            {
                var fileName = _path.GetFileName(file);
                var destinationFileName = _path.Combine(destinationPath, fileName);

                if (_file.Exists(destinationFileName) && !overwriteExisting)
                {
                    Log.Debug($"Skipping copying of '{file}', file already exists in target directory");
                    continue;
                }

                _file.Copy(file, destinationFileName, overwriteExisting);
            }

            if (copySubDirs)
            {
                var subDirectories = GetDirectories(sourcePath);

                foreach (var subDirectory in subDirectories)
                {
                    var subDirectoryName = _path.GetDirectoryName(subDirectory);
                    var destinationSubDirectory = _path.Combine(destinationPath, subDirectoryName);

                    Copy(subDirectory, destinationSubDirectory, copySubDirs, overwriteExisting);
                }
            }
        }

        public void Delete(string path, bool recursive = true)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                if (_directory.Exists(path))
                {
                    Log.Debug($"Deleting directory '{path}', recursive: '{recursive}'");

                    _directory.Delete(path, recursive);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to delete directory '{path}'");

                throw;
            }
        }

        public bool Exists(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            try
            {
                var exists = _directory.Exists(path);
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
                if (string.IsNullOrWhiteSpace(searchPattern))
                {
                    searchPattern = "*";
                }

                Log.Debug($"Getting directories inside '{path}', searchPattern: '{searchPattern}', searchOption: '{searchOption}'");

                var directories = _directory.GetDirectories(path, searchPattern, searchOption);
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
                if (string.IsNullOrWhiteSpace(searchPattern))
                {
                    searchPattern = "*";
                }

                Log.Debug($"Getting files inside '{path}', searchPattern: '{searchPattern}', searchOption: '{searchOption}'");

                var files = _directory.GetFiles(path, searchPattern, searchOption);
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
