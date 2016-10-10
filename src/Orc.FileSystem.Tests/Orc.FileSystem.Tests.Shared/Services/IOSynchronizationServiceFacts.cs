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
                using (var temporaryFilesContext = new TemporaryFilesContext("NestingBySameProcess"))
                {
                    var rootDirectory = temporaryFilesContext.GetDirectory("output");
                    var file1 = temporaryFilesContext.GetFile("output\\file1.txt");
                    var file2 = temporaryFilesContext.GetFile("output\\file2.txt");

                    var ioSynchronizationService = new IOSynchronizationService(new FileService());

                    await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async x =>
                    {
                        // File 1
                        await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async y =>
                        {
                            return true;
                        });

                        // File 2
                        await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async y =>
                        {
                            return true;
                        });

                        return true;
                    });
                }
            }

            [Test]
            public async Task AllowsAccessToNestingDirectoriesBySameProcessAsync()
            {
                using (var temporaryFilesContext = new TemporaryFilesContext("NestingBySameProcess"))
                {
                    var rootDirectory = temporaryFilesContext.GetDirectory("output");
                    var subdirectory = temporaryFilesContext.GetDirectory("output\\subdirectory");

                    var ioSynchronizationService = new IOSynchronizationService(new FileService());

                    await ioSynchronizationService.ExecuteWritingAsync(rootDirectory, async x =>
                    {
                        await ioSynchronizationService.ExecuteWritingAsync(subdirectory, async y =>
                        {
                            return true;
                        });

                        return true;
                    });
                }
            }
        }
    }
}