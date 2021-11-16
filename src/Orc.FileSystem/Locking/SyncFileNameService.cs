namespace Orc.FileSystem
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;

    public sealed class SyncFileNameService : ISyncFileNameService
    {
        private readonly IStringIdProvider _stringIdProvider;
        private readonly IPath _path;
        
        private const string SyncFileName = "__ofs";
        private const string SyncFileExtension = ".sync";
        private const string SyncFileReadSuffix = "r";
        private const string SyncFileWriteSuffix = "w";

        public SyncFileNameService(IStringIdProvider stringIdProvider, IFileSystem fileSystem)
        {
            _stringIdProvider = stringIdProvider;
            _path = fileSystem.Path;
        }

        public string GetFileName(FileLockScopeContext context)
        {
            var directoryName = context.DirectoryName;

            var fileName = BuildShortFileName(context);

            return _path.Combine(directoryName, fileName);
        }

        private string BuildShortFileName(FileLockScopeContext context)
        {
            var fileName = new StringBuilder(SyncFileName);
            if (context.IsReadScope is not null)
            {
                var suffix = context.IsReadScope.Value
                    ? SyncFileReadSuffix
                    : SyncFileWriteSuffix;

                fileName.Append("_").Append(suffix);
            }

            if (context.HasId)
            {
                var id = _stringIdProvider?.NewStringId();
                fileName.Append("#").Append(id);
            }

            fileName.Append(SyncFileExtension);

            return fileName.ToString();
        }

        public string GetFileSearchFilter(FileLockScopeContext context)
        {
            if (context.IsReadScope is null)
            {
                return $"{SyncFileName}*{SyncFileExtension}";
            }

            var suffix = context.IsReadScope.Value ? SyncFileReadSuffix : SyncFileWriteSuffix;
            var filter = $"{SyncFileName}_{suffix}*{SyncFileExtension}";
            return filter;
        }

        public FileLockScopeContext FileLockScopeContextFromFile(string fileName)
        {
            var context = new FileLockScopeContext();

            var extension = _path.GetExtension(fileName);
            if (!string.Equals(extension, SyncFileExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                // incorrect extension
                return context;
            }

            if (_path.IsPathRooted(fileName))
            {
                context.DirectoryName = _path.GetDirectoryName(fileName);
            }

            fileName = _path.GetFileNameWithoutExtension(fileName);
            if (fileName.Length < SyncFileExtension.Length)
            {
                // incorrect extension
                return context;
            }

            if (!fileName.StartsWith(SyncFileName))
            {
                // incorrect file name format
                return context;
            }

            fileName = fileName.Substring(SyncFileName.Length);

            var idIndex = fileName.IndexOf('#');
            if (idIndex >= 0)
            {
                context.HasId = true;
                var id = fileName.Substring(idIndex);
                // TODO: validate id

                fileName = fileName.Substring(0, idIndex);
            }

            if (string.IsNullOrEmpty(fileName))
            {
                return context;
            }

            switch (fileName.ToLower())
            {
                case "_r":
                    context.IsReadScope = true;
                    break;

                case "_w":
                    context.IsReadScope = false;
                    break;

                default:
                    // incorrect file name format
                    return context;
            }

            return context;
        }
    }
}
