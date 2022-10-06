namespace Orc.FileSystem
{
    using System.IO;

    public interface IDirectoryService
    {
        string Create(string path);
        void Move(string sourcePath, string destinationPath);
        void Delete(string path, bool recursive = true);
        bool Exists(string path);
        string[] GetDirectories(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        string[] GetFiles(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        void Copy(string sourcePath, string destinationPath, bool copySubDirs = true, bool overwriteExisting = false);
    }
}
