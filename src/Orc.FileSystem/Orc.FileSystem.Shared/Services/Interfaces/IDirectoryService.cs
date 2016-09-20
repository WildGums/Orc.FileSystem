// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDirectoryService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


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
    }
}