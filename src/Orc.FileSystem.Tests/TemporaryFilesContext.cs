namespace Orc.FileSystem.Tests
{
    using System;
    using System.IO;
    using Catel.Logging;

    public sealed class TemporaryFilesContext : IDisposable
    {
        #region Constants
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Fields
        private readonly Guid _randomGuid = Guid.NewGuid();
        private readonly string _rootDirectory;
        #endregion

        #region Constructors
        public TemporaryFilesContext(string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = _randomGuid.ToString();
            }

            _rootDirectory = Path.Combine(Path.GetTempPath(), GetType().Assembly.GetName().Name, name);

            Directory.CreateDirectory(_rootDirectory);
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            Log.Debug("Deleting temporary files from '{0}'", _rootDirectory);

            try
            {
                if (Directory.Exists(_rootDirectory))
                {
                    Directory.Delete(_rootDirectory, true);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to delete temporary files");
            }
        }
        #endregion

        #region Methods
        public string GetDirectory(string relativeDirectoryName)
        {
            var fullPath = Path.Combine(_rootDirectory, relativeDirectoryName);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            return fullPath;
        }

        public string GetFile(string relativeFilePath, bool deleteIfExists = false)
        {
            var fullPath = Path.Combine(_rootDirectory, relativeFilePath);

            var directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (deleteIfExists)
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }

            return fullPath;
        }
        #endregion
    }
}
