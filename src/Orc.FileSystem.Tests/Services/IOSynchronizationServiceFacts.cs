// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOSynchronizationServiceFacts.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem.Tests.Services
{
    using System;
    using System.IO;
    using System.IO.Abstractions.TestingHelpers;
    using System.Threading.Tasks;
    using Catel.Threading;
    using NUnit.Framework;

    public class IOSynchronizationServiceFacts
    {
        // TODO: Write unit tests

        [TestFixture]
        public class TheExecuteWritingAsyncMethod
        {
            [Test]
            public async Task WriterWrapsAnyExceptionIntoIOSynchronizationExceptionAsync()
            {
                try
                {
                    using (var temporaryFilesContext = new TemporaryFilesContext("DoesNotSwallowReaderIOExceptionAsync"))
                    {
                        var rootDirectory = temporaryFilesContext.GetDirectory("output");

                        var fileService = new FileService(new MockFileSystem());
                        var ioSynchronizationService = new IOSynchronizationService(fileService, new DirectoryService(fileService));

                        var aleadyExecuted = false;

                        await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async x =>
                        {
                            if (!aleadyExecuted)
                            {
                                // preventing continuous loop
                                aleadyExecuted = true;

                                throw new IOException();
                            }

                            return true;
                        });
                    }

                    Assert.Fail("Expected exception");
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOf<IOSynchronizationException>(ex);
                }
            }

            [Test]
            public async Task AllowsAccessToSameDirectoryBySameProcessAsync()
            {
                using (var temporaryFilesContext = new TemporaryFilesContext("AllowsAccessToSameDirectoryBySameProcessAsync"))
                {
                    var rootDirectory = temporaryFilesContext.GetDirectory("output");
                    var file1 = temporaryFilesContext.GetFile("output\\file1.txt");
                    var file2 = temporaryFilesContext.GetFile("output\\file2.txt");

                    var fileService = new FileService(new MockFileSystem());
                    var ioSynchronizationService = new IOSynchronizationService(fileService, new DirectoryService(fileService));

                    await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async x =>
                    {
                        // File 1
                        await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async y => true);

                        // File 2
                        await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async y => true);

                        return true;
                    });
                }
            }

            [Test]
            public async Task AllowsAccessToNestingDirectoriesBySameProcessAsync()
            {
                using (var temporaryFilesContext = new TemporaryFilesContext("AllowsAccessToNestingDirectoriesBySameProcessAsync"))
                {
                    var rootDirectory = temporaryFilesContext.GetDirectory("output");
                    var subdirectory = temporaryFilesContext.GetDirectory("output\\subdirectory");

                    var fileService = new FileService(new MockFileSystem());
                    var ioSynchronizationService = new IOSynchronizationService(fileService, new DirectoryService(fileService));

                    await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async x =>
                    {
                        await ioSynchronizationService.ExecuteWritingAsync(subdirectory, async y => true);

                        return true;
                    });
                }
            }
        }

        [TestFixture]
        public class TheExecuteReadingAsyncMethod
        {
            [Test]
            public async Task ReaderWrapsAnyExceptionIntoIOSynchronizationExceptionAsync()
            {
                try
                {
                    using (var temporaryFilesContext = new TemporaryFilesContext("DoesNotSwallowReaderIOExceptionAsync"))
                    {
                        var rootDirectory = temporaryFilesContext.GetDirectory("output");
                        var fileName = temporaryFilesContext.GetFile("file1.txt");

                        var fileService = new FileService(new MockFileSystem());
                        var ioSynchronizationService = new IOSynchronizationService(fileService, new DirectoryService(fileService));

                        // write for creating sync file
                        await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async x =>
                        {
                            await File.WriteAllTextAsync(fileName, "12345");
                            return true;
                        });

                        var aleadyExecuted = false;

                        await ioSynchronizationService.ExecuteReadingAsync(rootDirectory, async x =>
                        {
                            if (!aleadyExecuted)
                            {
                                // preventing continuous loop
                                aleadyExecuted = true;

                                throw new IOException();
                            }

                            return true;
                        });
                    }

                    Assert.Fail("Expected exception");
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOf<IOSynchronizationException>(ex);
                }
            }

            [Test]
            public async Task DoesNotDeleteSyncFileIfEqualsToObservedFilePathAsync()
            {
                using (var temporaryFilesContext = new TemporaryFilesContext("DoesNotDeleteSyncFileIfEqualsToObservedFilePathAsync"))
                {
                    var fileName = temporaryFilesContext.GetFile("file1.txt");

                    var fileService = new FileService(new MockFileSystem());
                    var ioSynchronizationService = new IOSynchronizationWithoutSeparateSyncFileService(fileService, new DirectoryService(fileService));

                    // ensure syn file exists and data file exists
                    await ioSynchronizationService.ExecuteWritingAsync(fileName, async x =>
                    {
                        await File.WriteAllTextAsync(fileName, "12345");
                        return true;
                    });

                    var syncFile = ioSynchronizationService.GetSyncFileByPath(fileName);
                    // required thing
                    Assert.AreEqual(syncFile, fileName);

                    Assert.IsTrue(File.Exists(syncFile));

                    // nested readings
                    await ioSynchronizationService.ExecuteReadingAsync(fileName, async x =>
                    {
                        await ioSynchronizationService.ExecuteReadingAsync(fileName, async y =>
                        {
                            Assert.IsTrue(File.Exists(syncFile));

                            return true;
                        });

                        Assert.IsTrue(File.Exists(syncFile));

                        return true;
                    });

                    // Even now the refresh file should not be removed
                    Assert.IsTrue(File.Exists(syncFile));
                }
            }

            [Test]
            public async Task CorrectlyReleasesFileAfterAllNestedScopesHaveBeenReleasedAsync()
            {
                using (var temporaryFilesContext = new TemporaryFilesContext("CorrectlyReleasesFileAfterAllNestedScopesHaveBeenReleasedAsync"))
                {
                    var rootDirectory = temporaryFilesContext.GetDirectory("output");
                    var fileName = temporaryFilesContext.GetFile("file1.txt");

                    var fileService = new FileService(new MockFileSystem());
                    var ioSynchronizationService = new IOSynchronizationService(fileService, new DirectoryService(fileService));

                    // Step 1: Write
                    await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async x =>
                    {
                        await File.WriteAllTextAsync(fileName, "12345");
                        return true;
                    });

                    var syncFile = ioSynchronizationService.GetSyncFileByPath(rootDirectory);

                    Assert.IsTrue(File.Exists(syncFile));

                    // Now do 2 nested reads
                    await ioSynchronizationService.ExecuteReadingAsync(rootDirectory, async x =>
                    {
                        await ioSynchronizationService.ExecuteReadingAsync(rootDirectory, async y =>
                        {
                            Assert.IsTrue(File.Exists(syncFile));

                            return true;
                        });

                        Assert.IsTrue(File.Exists(syncFile));

                        return true;
                    });

                    // Only now the refresh file should be removed
                    Assert.IsFalse(File.Exists(syncFile));
                }
            }

            [Test]
            public async Task WaitWithReadingUntilWriteIsFinishedAsync()
            {
                using (var temporaryFilesContext = new TemporaryFilesContext("WaitWithReadingUntilWriteIsFinishedAsync"))
                {
                    var rootDirectory = temporaryFilesContext.GetDirectory("output");
                    var fileName = temporaryFilesContext.GetFile("file1.txt");

                    var fileService = new FileService(new MockFileSystem());
                    var ioSynchronizationService = new IOSynchronizationService(fileService, new DirectoryService(fileService));

                    // Step 1: Write, do not await
#pragma warning disable 4014
                    ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async x =>
#pragma warning restore 4014
                    {
                        await File.WriteAllTextAsync(fileName, "12345");

                        await TaskShim.Delay(2500);

                        return true;
                    });

                    var startTime = DateTime.Now;
                    var endTime = DateTime.Now;

                    // Step 2: read, but should only be allowed after 5 seconds
                    await ioSynchronizationService.ExecuteReadingAsync(rootDirectory, async y =>
                    {
                        endTime = DateTime.Now;
                        return true;
                    });

                    var delta = endTime - startTime;

                    // Delta should be at least 2 seconds (meaning we have awaited the writing)
                    Assert.IsTrue(delta > TimeSpan.FromSeconds(2));
                }
            }
        }
    }
}
