# Orc.FileSystem

[![Join the chat at https://gitter.im/WildGums/Orc.FileSystem](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/WildGums/Orc.FileSystem?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

![License](https://img.shields.io/github/license/WildGums/Orc.FileSystem.svg)
![NuGet downloads](https://img.shields.io/nuget/dt/Orc.FileSystem.svg)
![Version](https://img.shields.io/nuget/v/[NUGET.PACKAGENAME].svg)
![Pre-release version](https://img.shields.io/nuget/vpre/Orc.FileSystem.svg)

This library wraps file system methods inside services. The advantages are:

- All operations are being logged and can easily be accessed (even in production scenarios)
- All operations are wrapped inside try/catch so all failures are logged as well
- Services allow easier mocking for unit tests

# FileService

The `FileService` provides the following methods:

- FileStream Create(string fileName)
- void Copy(string sourceFileName, string destinationFileName, bool overwrite = false)
- void Move(string sourceFileName, string destinationFileName, bool overwrite = false)
- bool Exists(string fileName)
- void Delete(string fileName)
- FileStream Open(string fileName, FileMode fileMode, FileAccess fileAccess = FileAccess.ReadWrite)

# DirectoryService

The `DirectoryService` provides the following methods:

- string Create(string path)
- void Move(string sourcePath, string destinationPath)
- void Delete(string path, bool recursive)
- bool Exists(string path)
- string[] GetDirectories(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
- string[] GetFiles(string path, string searchPattern = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)

# FileLocker
The `FileLocker` is a disposable class which provides file read/write synchronization for several concurrent processes. It provides the following methods:

- Task LockFilesAsync(params string[] files)
- Task LockFilesAsync(TimeSpan timeout, params string[] files)
