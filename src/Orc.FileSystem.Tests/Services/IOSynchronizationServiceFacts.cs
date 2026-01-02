namespace Orc.FileSystem.Tests.Services;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

public class IOSynchronizationServiceFacts
{
    [TestFixture]
    public class TheExecuteWritingAsyncMethod
    {
        [Test]
        public async Task Writer_Wraps_Any_Exception_Into_IOSynchronizationException()
        {
            try
            {
                using (var temporaryFilesContext = new TemporaryFilesContext("DoesNotSwallowReaderIOExceptionAsync"))
                {
                    var rootDirectory = temporaryFilesContext.GetDirectory("output");

                    var fileService = new FileService(NullLogger<FileService>.Instance);
                    var ioSynchronizationService = new IOSynchronizationService(NullLogger<IOSynchronizationService>.Instance, 
                        fileService, new DirectoryService(NullLogger<DirectoryService>.Instance, fileService));

                    var alreadyExecuted = false;

                    await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async _ =>
                    {
                        if (alreadyExecuted)
                        {
                            return true;
                        }

                        // preventing continuous loop
                        alreadyExecuted = true;

                        throw new IOException();

                    });
                }

                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                Assert.That(ex, Is.InstanceOf<IOSynchronizationException>());
            }
        }

        [Test]
        public async Task Allows_Access_To_Same_Directory_By_Same_Process()
        {
            using var temporaryFilesContext = new TemporaryFilesContext("AllowsAccessToSameDirectoryBySameProcessAsync");
            var rootDirectory = temporaryFilesContext.GetDirectory("output");
            var file1 = temporaryFilesContext.GetFile("output\\file1.txt");
            var file2 = temporaryFilesContext.GetFile("output\\file2.txt");

            var fileService = new FileService(NullLogger<FileService>.Instance);
            var ioSynchronizationService = new IOSynchronizationService(NullLogger<IOSynchronizationService>.Instance,
                fileService, new DirectoryService(NullLogger<DirectoryService>.Instance, fileService));

            await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async _ =>
            {
                // File 1
                await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async _ => true);

                // File 2
                await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async _ => true);

                return true;
            });
        }

        [Test]
        public async Task Allows_Access_To_Nesting_Directories_By_Same_Process()
        {
            using var temporaryFilesContext = new TemporaryFilesContext("AllowsAccessToNestingDirectoriesBySameProcessAsync");
            var rootDirectory = temporaryFilesContext.GetDirectory("output");
            var subDirectory = temporaryFilesContext.GetDirectory("output\\subdirectory");

            var fileService = new FileService(NullLogger<FileService>.Instance);
            var ioSynchronizationService = new IOSynchronizationService(NullLogger<IOSynchronizationService>.Instance,
                fileService, new DirectoryService(NullLogger<DirectoryService>.Instance, fileService));

            await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async _ =>
            {
                await ioSynchronizationService.ExecuteWritingAsync(subDirectory, async _ => true);

                return true;
            });
        }
    }

    [TestFixture]
    public class The_ExecuteReadingAsync_Method
    {
        [Test]
        public async Task Reader_Wraps_Any_Exception_Into_IOSynchronizationException()
        {
            try
            {
                using (var temporaryFilesContext = new TemporaryFilesContext("DoesNotSwallowReaderIOExceptionAsync"))
                {
                    var rootDirectory = temporaryFilesContext.GetDirectory("output");
                    var fileName = temporaryFilesContext.GetFile("file1.txt");

                    var fileService = new FileService(NullLogger<FileService>.Instance);
                    var ioSynchronizationService = new IOSynchronizationService(NullLogger<IOSynchronizationService>.Instance,
                        fileService, new DirectoryService(NullLogger<DirectoryService>.Instance, fileService));

                    // write for creating sync file
                    await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async _ =>
                    {
                        await File.WriteAllTextAsync(fileName, "12345");
                        return true;
                    });

                    var alreadyExecuted = false;

                    await ioSynchronizationService.ExecuteReadingAsync(rootDirectory, async _ =>
                    {
                        if (alreadyExecuted)
                        {
                            return true;
                        }

                        // preventing continuous loop
                        alreadyExecuted = true;

                        throw new IOException();

                    });
                }

                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                Assert.That(ex, Is.InstanceOf<IOSynchronizationException>());
            }
        }

        [Test]
        public async Task Does_Not_Delete_Sync_File_If_Equals_To_Observed_File_Path()
        {
            using var temporaryFilesContext = new TemporaryFilesContext("DoesNotDeleteSyncFileIfEqualsToObservedFilePathAsync");
            var fileName = temporaryFilesContext.GetFile("file1.txt");

            var fileService = new FileService(NullLogger<FileService>.Instance);
            var ioSynchronizationService = new IOSynchronizationWithoutSeparateSyncFileService(NullLogger<IOSynchronizationService>.Instance,
                fileService, new DirectoryService(NullLogger<DirectoryService>.Instance, fileService));

            // ensure syn file exists and data file exists
            await ioSynchronizationService.ExecuteWritingAsync(fileName, async _ =>
            {
                await File.WriteAllTextAsync(fileName, "12345");
                return true;
            });

            var syncFile = ioSynchronizationService.GetSyncFileByPath(fileName);
            // required thing
            Assert.That(fileName, Is.EqualTo(syncFile));

            Assert.That(File.Exists(syncFile), Is.True);

            // nested readings
            await ioSynchronizationService.ExecuteReadingAsync(fileName, async _ =>
            {
                await ioSynchronizationService.ExecuteReadingAsync(fileName, async _ =>
                {
                    Assert.That(File.Exists(syncFile), Is.True);

                    return true;
                });

                Assert.That(File.Exists(syncFile), Is.True);

                return true;
            });

            // Even now the refresh file should not be removed
            Assert.That(File.Exists(syncFile), Is.True);
        }

        [Test]
        public async Task Correctly_Releases_File_After_All_Nested_Scopes_Have_Been_Released()
        {
            using var temporaryFilesContext = new TemporaryFilesContext("CorrectlyReleasesFileAfterAllNestedScopesHaveBeenReleasedAsync");
            var rootDirectory = temporaryFilesContext.GetDirectory("output");
            var fileName = temporaryFilesContext.GetFile("file1.txt");

            var fileService = new FileService(NullLogger<FileService>.Instance);
            var ioSynchronizationService = new IOSynchronizationService(NullLogger<IOSynchronizationService>.Instance,
                fileService, new DirectoryService(NullLogger<DirectoryService>.Instance, fileService));

            // Step 1: Write
            await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async _ =>
            {
                await File.WriteAllTextAsync(fileName, "12345");
                return true;
            });

            var syncFile = ioSynchronizationService.GetSyncFileByPath(rootDirectory);

            Assert.That(File.Exists(syncFile), Is.True);

            // Now do 2 nested reads
            await ioSynchronizationService.ExecuteReadingAsync(rootDirectory, async _ =>
            {
                await ioSynchronizationService.ExecuteReadingAsync(rootDirectory, async _ =>
                {
                    Assert.That(File.Exists(syncFile), Is.True);

                    return true;
                });

                Assert.That(File.Exists(syncFile), Is.True);

                return true;
            });

            // Only now the refresh file should be removed
            Assert.That(File.Exists(syncFile), Is.False);
        }

        [Test]
        public async Task Wait_With_Reading_Until_Write_Is_Finished()
        {
            using var temporaryFilesContext = new TemporaryFilesContext("WaitWithReadingUntilWriteIsFinishedAsync");
            var rootDirectory = temporaryFilesContext.GetDirectory("output");
            var fileName = temporaryFilesContext.GetFile("file1.txt");

            var fileService = new FileService(NullLogger<FileService>.Instance);
            var ioSynchronizationService = new IOSynchronizationService(NullLogger<IOSynchronizationService>.Instance,
                fileService, new DirectoryService(NullLogger<DirectoryService>.Instance, fileService));

            // Step 1: Write, do not await
#pragma warning disable 4014
            ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async _ =>
#pragma warning restore 4014
            {
                await File.WriteAllTextAsync(fileName, "12345");

                await Task.Delay(2500);

                return true;
            });

            var startTime = DateTime.Now;
            var endTime = DateTime.Now;

            // Step 2: read, but should only be allowed after 5 seconds
            await ioSynchronizationService.ExecuteReadingAsync(rootDirectory, async _ =>
            {
                endTime = DateTime.Now;
                return true;
            });

            var delta = endTime - startTime;

            // Delta should be at least 2 seconds (meaning we have awaited the writing)
            Assert.That(delta > TimeSpan.FromSeconds(2), Is.True);
        }
    }
}
