namespace Orc.FileSystem.Tests.Locking
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.IO.Abstractions.TestingHelpers;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class SyncFileNameServiceFacts
    {
        private IFileSystem _fileSystem;
        
        [SetUp]
        public void SetupTest()
        {
            _fileSystem = new MockFileSystem();
        }

        [Test]
        public void CanCreateCorrectReadingSyncFileNameWithoutId()
        {
            var syncFileNameService = new SyncFileNameService(It.IsAny<IStringIdProvider>(), _fileSystem);
            var info = new FileLockScopeContext()
            {
                DirectoryName = @"C:\",
                IsReadScope = true,
                HasId = false
            };

            var fileName = syncFileNameService.GetFileName(info);

            Assert.AreEqual($@"C:\__ofs_r.sync", fileName);
        }

        [Test]
        public void CanCreateCorrectWritingSyncFileNameWithoutId()
        {
            var syncFileNameService = new SyncFileNameService(It.IsAny<IStringIdProvider>(), _fileSystem);
            var info = new FileLockScopeContext()
            {
                DirectoryName = @"C:\",
                IsReadScope = false,
                HasId = false
            };

            var fileName = syncFileNameService.GetFileName(info);

            Assert.AreEqual($@"C:\__ofs_w.sync", fileName);
        }

        [Test]
        public void CanCreateCorrectGeneralSyncFileNameWithoutId()
        {
            var syncFileNameService = new SyncFileNameService(It.IsAny<IStringIdProvider>(), _fileSystem);
            var info = new FileLockScopeContext()
            {
                DirectoryName = @"C:\",
                IsReadScope = null,
                HasId = false
            };

            var fileName = syncFileNameService.GetFileName(info);

            Assert.AreEqual($@"C:\__ofs.sync", fileName);
        }

        [TestCase("id")]
        [TestCase("identifier")]
        public void CanCreateCorrectGeneralSyncFileNameWithId(string id)
        {
            var stringIdProviderMock = MockStringIdProvider(id);

            var syncFileNameService = new SyncFileNameService(stringIdProviderMock.Object, _fileSystem);
            var info = new FileLockScopeContext()
            {
                DirectoryName = @"C:\",
                IsReadScope = null,
                HasId = true
            };

            var fileName = syncFileNameService.GetFileName(info);

            Assert.AreEqual($@"C:\__ofs#{id}.sync", fileName);
        }

        [TestCase("id")]
        [TestCase("identifier")]
        public void CanCreateCorrectReadingSyncFileNameWithIdForDirectory(string id)
        {
            var stringIdProviderMock = MockStringIdProvider(id);

            var syncFileNameService = new SyncFileNameService(stringIdProviderMock.Object, _fileSystem);
            var info = new FileLockScopeContext()
            {
                DirectoryName = @"C:\",
                IsReadScope = true,
                HasId = true,
            };

            var fileName = syncFileNameService.GetFileName(info);

            Assert.AreEqual($@"C:\__ofs_r#{id}.sync", fileName);
        }

        [TestCase("id")]
        [TestCase("identifier")]
        public void CanCreateCorrectWritingSyncFileNameWithId(string id)
        {
            var stringIdProviderMock = MockStringIdProvider(id);
            var syncFileNameService = new SyncFileNameService(stringIdProviderMock.Object, _fileSystem);

            var info = new FileLockScopeContext()
            {
                DirectoryName = @"C:\",
                IsReadScope = false,
                HasId = true,
            };

            var fileName = syncFileNameService.GetFileName(info);

            Assert.AreEqual($@"C:\__ofs_w#{id}.sync", fileName);
        }

        [Test]
        public void CanGetFileSearchFilterFromWritingContextWithId()
        {
            var stringIdProviderMock = MockStringIdProvider(It.IsAny<string>());
            var syncFileNameService = new SyncFileNameService(stringIdProviderMock.Object, _fileSystem);

            var info = new FileLockScopeContext()
            {
                DirectoryName = null,
                IsReadScope = false
            };

            var fileSearchFilter = syncFileNameService.GetFileSearchFilter(info);

            Assert.AreEqual("__ofs_w*.sync", fileSearchFilter);
        }

        [Test]
        public void CanGetFileSearchFilterFromReadingContext()
        {
            var stringIdProviderMock = MockStringIdProvider(It.IsAny<string>());
            var syncFileNameService = new SyncFileNameService(stringIdProviderMock.Object, _fileSystem);

            var info = new FileLockScopeContext() { DirectoryName = It.IsAny<string>(), IsReadScope = true };

            var fileSearchFilter = syncFileNameService.GetFileSearchFilter(info);

            Assert.AreEqual("__ofs_r*.sync", fileSearchFilter);
        }

        [Test]
        public void CanGetFileSearchFilterFromGeneralContext()
        {
            var stringIdProviderMock = MockStringIdProvider(It.IsAny<string>());
            var syncFileNameService = new SyncFileNameService(stringIdProviderMock.Object, _fileSystem);

            var info = new FileLockScopeContext()
            {
                DirectoryName = null,
                IsReadScope = null
            };

            var fileSearchFilter = syncFileNameService.GetFileSearchFilter(info);

            Assert.AreEqual("__ofs*.sync", fileSearchFilter);
        }

        [Test]
        public void CanGetFileLockScopeInfoFromWritingSyncFileNameWithId()
        {
            var syncFileNameService = new SyncFileNameService(It.IsAny<IStringIdProvider>(), _fileSystem);

            var fileLockScopeContext = syncFileNameService.FileLockScopeContextFromFile($@"C:\__ofs_w#id.sync");

            Assert.AreEqual(fileLockScopeContext.HasId, true);
            Assert.AreEqual(fileLockScopeContext.DirectoryName, @"C:\");
            Assert.AreEqual(fileLockScopeContext.IsReadScope, false);
        }

        [Test]
        public void CanGetFileLockScopeInfoFromReadingSyncFileNameWithId()
        {
            var syncFileNameService = new SyncFileNameService(It.IsAny<IStringIdProvider>(), _fileSystem);

            var fileLockScopeContext = syncFileNameService.FileLockScopeContextFromFile($@"C:\__ofs_r#id.sync");

            Assert.AreEqual(fileLockScopeContext.HasId, true);
            Assert.AreEqual(fileLockScopeContext.DirectoryName, @"C:\");
            Assert.AreEqual(fileLockScopeContext.IsReadScope, true);
        }

        [Test]
        public void CanGetFileLockScopeInfoFromSyncFileNameWithoutId()
        {
            var syncFileNameService = new SyncFileNameService(It.IsAny<IStringIdProvider>(), _fileSystem);

            var fileLockScopeContext = syncFileNameService.FileLockScopeContextFromFile($@"C:\__ofs.sync");

            Assert.AreEqual(fileLockScopeContext.HasId, false);
            Assert.AreEqual(fileLockScopeContext.DirectoryName, @"C:\");
            Assert.AreEqual(fileLockScopeContext.IsReadScope, null);
        }

        [Test]
        public void CanGetFileLockScopeInfoFromFileName()
        {
            var syncFileNameService = new SyncFileNameService(It.IsAny<IStringIdProvider>(), _fileSystem);

            var info = new FileLockScopeContext()
            {
                DirectoryName = null,
                IsReadScope = null
            };

            var fileLockScopeContext = syncFileNameService.FileLockScopeContextFromFile($@"C:\__ofs_w#id.sync");

            Assert.AreEqual(fileLockScopeContext.HasId, true);
            Assert.AreEqual(fileLockScopeContext.DirectoryName, @"C:\");
            Assert.AreEqual(fileLockScopeContext.IsReadScope, false);
        }

        private static Mock<IStringIdProvider> MockStringIdProvider(string id)
        {
            var stringIdProviderMock = new Mock<IStringIdProvider>();
            stringIdProviderMock.Setup(x => x.NewStringId())
                .Returns(id);
            return stringIdProviderMock;
        }
    }
}
