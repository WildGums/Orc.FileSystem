// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.IO;
    using Catel;
    using Catel.Logging;

    public class FileService : IFileService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public FileStream Create(string fileName)
        {
            Argument.IsNotNullOrWhitespace(() => fileName);

            Log.Info($"Creating file '{fileName}'");

            try
            {
                var fileStream = File.Create(fileName);
                return fileStream;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to create file '{fileName}'");

                throw;
            }
        }

        public void Copy(string sourceFileName, string destinationFileName, bool overwrite = false)
        {
            Argument.IsNotNullOrWhitespace(() => sourceFileName);
            Argument.IsNotNullOrWhitespace(() => destinationFileName);

            Log.Info($"Copying file '{sourceFileName}' => '{destinationFileName}', overwrite: '{overwrite}'");

            try
            {
                File.Copy(sourceFileName, destinationFileName, overwrite);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to copy file '{sourceFileName}' => '{destinationFileName}'");

                throw;
            }
        }

        public void Move(string sourceFileName, string destinationFileName, bool overwrite = false)
        {
            Argument.IsNotNullOrWhitespace(() => sourceFileName);
            Argument.IsNotNullOrWhitespace(() => destinationFileName);

            Log.Info($"Moving file '{sourceFileName}' => '{destinationFileName}', overwrite: '{overwrite}'");

            try
            {
                if (File.Exists(sourceFileName))
                {
                    if (File.Exists(destinationFileName) && overwrite)
                    {
                        File.Delete(destinationFileName);
                    }
                }

                File.Move(sourceFileName, destinationFileName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to move file '{sourceFileName}' => '{destinationFileName}'");

                throw;
            }
        }

        public bool Exists(string fileName)
        {
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                var exists = File.Exists(fileName);
                return exists;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to check whether file '{fileName}' exists");

                throw;
            }
        }

        public void Delete(string fileName)
        {
            Argument.IsNotNullOrWhitespace(() => fileName);

            try
            {
                if (File.Exists(fileName))
                {
                    Log.Info($"Deleting file '{fileName}'");

                    File.Delete(fileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to delete file '{fileName}'");

                throw;
            }
        }
    }
}