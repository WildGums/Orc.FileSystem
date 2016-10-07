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

# IOSynchronizationService

The `IOSynchronizerService` can take care of synchronized blocks of reading and/or write to a specific directory or file. This provides an easy way to "lock" a directory or file until the director/file has been released. For example, when writing several files that need to lock a directory until all files are written, this class can come in handy. The examples below all use a `projectDirectory` variable to use as base path. This will also be the path to be locked.

## Start watching for changes

	ioSynchronizationService.RefreshRequired += OnIoSynchronizationServiceRefreshRequired;
	await ioSynchronizationService.StartWatchingForChangesAsync(projectDirectory);

## Writing files

The writing of the files can happen in a completely different app, the services will take care of the synchronization automatically. To write files, use the following code:

	var file1 = Path.Combine(projectDirectory, "file1.txt");
	var file2 = Path.Combine(projectDirectory, "file2.txt");
	
	await ioSynchronizationService.ExecuteWritingAsync(projectDirectory, async x => 
	{
	    fileService.WriteAllText(file1, "sample content");
		fileService.WriteAllText(file2, "sample content");
	
	    return true;
	});

## Reading files

To read files, use the following code:

	var file1 = Path.Combine(projectDirectory, "file1.txt");
	var file2 = Path.Combine(projectDirectory, "file2.txt");
	
	var file1Contents = string.Empty;
	var file2Contents = string.Empty;
	
	await ioSynchronizationService.ExecuteReadingAsync(projectDirectory, async x => 
	{
	    file1Contents = fileService.ReadAllText(file1);
		file2Contents = fileService.ReadAllText(file2);
	
	    return true;
	});





