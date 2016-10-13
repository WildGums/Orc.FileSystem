// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOSynchronizationServiceFacts.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem.Tests.Services
{
    using System.IO;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class IOSynchronizationServiceFacts
    {
        // TODO: Write unit tests

        [TestFixture]
        public class TheExecuteWritingAsyncMethod
        {
            [Test]
            public async Task AllowsAccessToSameDirectoryBySameProcessAsync()
            {
                using (var temporaryFilesContext = new TemporaryFilesContext("AllowsAccessToSameDirectoryBySameProcessAsync"))
                {
                    var rootDirectory = temporaryFilesContext.GetDirectory("output");
                    var file1 = temporaryFilesContext.GetFile("output\\file1.txt");
                    var file2 = temporaryFilesContext.GetFile("output\\file2.txt");

                    var ioSynchronizationService = new IOSynchronizationService(new FileService());

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

                    var ioSynchronizationService = new IOSynchronizationService(new FileService());

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
            public async Task CorrectlyReleasesFileAfterAllNestedScopesHaveBeenReleasedAsync()
            {
                using (var temporaryFilesContext = new TemporaryFilesContext("CorrectlyReleasesFileAfterAllNestedScopesHaveBeenReleasedAsync"))
                {
                    var rootDirectory = temporaryFilesContext.GetDirectory("output");
                    var fileName = temporaryFilesContext.GetFile("file1.txt");

                    var ioSynchronizationService = new IOSynchronizationService(new FileService());

                    // Step 1: Write
                    await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async x =>
                    {
                        File.WriteAllText(fileName, "12345");
                        return true;
                    });

                    var refreshFile = ioSynchronizationService.GetRefreshFileByPath(rootDirectory);

                    Assert.IsTrue(File.Exists(refreshFile));

                    // Now do 2 nested reads
                    await ioSynchronizationService.ExecuteReadingAsync(rootDirectory, async x =>
                    {
                        await ioSynchronizationService.ExecuteReadingAsync(rootDirectory, async y =>
                        {
                            Assert.IsTrue(File.Exists(refreshFile));

                            return true;
                        });

                        Assert.IsTrue(File.Exists(refreshFile));

                        return true;
                    });

                    // Only now the refresh file should be removed
                    Assert.IsFalse(File.Exists(refreshFile));
                }
            }
        }
    }
}