﻿namespace Orc.FileSystem;

using System.IO;

public interface IFileService
{
    Stream Create(string fileName);
    void Copy(string sourceFileName, string destinationFileName, bool overwrite = false);
    void Move(string sourceFileName, string destinationFileName, bool overwrite = false);
    bool Exists(string fileName);
    void Delete(string fileName);
    Stream Open(string fileName, FileMode fileMode, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.ReadWrite);
    bool CanOpen(string fileName, FileMode fileMode, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.ReadWrite);
}