namespace Orc.FileSystem.Tests.Locking
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.IO.Abstractions.TestingHelpers;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class FileLockScopeFacts
    {
        private static readonly string RootPath = Path.Combine(Path.GetTempPath(), "Orc.FileSystem.Tests");

        private IStringIdProvider _stringIdProvider;
        private ISyncFileNameService _syncFileNameService;
        private MockFileSystem _fileSystem;

        public class FileLockTestContext
        {
            public bool? IsReadScope { get; set; }
            public int ReleasedSyncFiles { get; set; }
            public int BusySyncFiles { get; set; }
        }

        private IFileLockScope MockFileLockScope(FileLockTestContext testContext)
        {
            _stringIdProvider = new StringIdProvider();
            _fileSystem = new MockFileSystem();
            _syncFileNameService = new SyncFileNameService(_stringIdProvider, _fileSystem);

            var context = new FileLockScopeContext() { DirectoryName = @"C:\", HasId = true, IsReadScope = testContext.IsReadScope };

            for (int i = 0; i < testContext.ReleasedSyncFiles; i++)
            {
                var fileName = _syncFileNameService.GetFileName(context);
                using var stream = _fileSystem.File.Create(fileName);
            }

            for (int i = 0; i < testContext.BusySyncFiles; i++)
            {
                var fileName = _syncFileNameService.GetFileName(context);
                // Note: keep file locked
                var stream = _fileSystem.File.Create(fileName);
            }

            return new FileLockScope(context, _syncFileNameService, _fileSystem);
        }

        [Test]
        public void CanCreateReadingLocker()
        {
            // Arrange:     No lock files exist
            var testContext = new FileLockTestContext { IsReadScope = true };
            var fileLockScope = MockFileLockScope(testContext);

            var filesBeforeAction = _fileSystem.AllFiles.ToList();

            // Act:         Try to create lock file for Reading
            var isLocked = fileLockScope.Lock();

            // Assert:      The lock file successfully created
            Assert.IsTrue(isLocked);

            var delta = _fileSystem.AllFiles.Except(filesBeforeAction).ToList();
            Assert.AreEqual(1, delta.Count());

            var context = _syncFileNameService.FileLockScopeContextFromFile(delta.First());
            Assert.IsTrue(context.IsReadScope);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CanCreateAnyLocker(bool? isReadScope)
        {
            // Arrange:     No lock files exist
            var testContext = new FileLockTestContext { IsReadScope = false };
            var fileLockScope = MockFileLockScope(testContext);

            var filesBeforeAction = _fileSystem.AllFiles.ToList();

            // Act:         Try to create lock file for WRITING
            var isLocked = fileLockScope.Lock();

            // Assert:      The lock file successfully created
            Assert.IsTrue(isLocked);

            var delta = _fileSystem.AllFiles.Except(filesBeforeAction).ToList();
            Assert.AreEqual(1, delta.Count());

            var context = _syncFileNameService.FileLockScopeContextFromFile(delta.First());
            Assert.IsFalse(context.IsReadScope);
        }

        [TestCase(true)]
        [TestCase(false)]
        [TestCase(null)]
        public void CanCreateAnyLockerWhenAbandonedLockerExists(bool? isReadScope)
        {
            // Arrange:     Reading lock file exists but not used by any process (probably left after crashing)
            var testContext = new FileLockTestContext { IsReadScope = isReadScope, ReleasedSyncFiles = 2, };

            var fileLockScope = MockFileLockScope(testContext);
            var filesBeforeAction = _fileSystem.AllFiles.ToList();

            // Act:         Try to create another lock file for WRITING
            var isLocked = fileLockScope.Lock();

            // Assert:      The lock file successfully created
            Assert.IsTrue(isLocked);

            var filesAfterAction = _fileSystem.AllFiles.ToList();
            var delta = filesAfterAction.Except(filesBeforeAction).ToList();
            Assert.AreEqual(1, delta.Count());

            var context = _syncFileNameService.FileLockScopeContextFromFile(delta.First());
            Assert.AreEqual(isReadScope, context.IsReadScope);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AutoDeleteLockerFileAfterUnlocking(bool isReadScope)
        {
            // Arrange:     Reading lock file exists but not used by any process (probably left after crashing)
            var testContext = new FileLockTestContext { IsReadScope = isReadScope, };

            var fileLockScope = MockFileLockScope(testContext);
            var filesBeforeAction = _fileSystem.AllFiles.ToList();

            // Act:         Create another lock file for READING and close locking
            var isLocked = fileLockScope.Lock();
            fileLockScope.Unlock();

            // Assert:      All the lock file has been deleted
            Assert.IsTrue(isLocked);

            var fileAfterAction = _fileSystem.AllFiles.ToList();
            Assert.AreEqual(0, fileAfterAction.Count);
        }

        [Test]
        public void AutoDeleteAbandonedLockersWhenCreatingNewOne()
        {
            // Arrange:     Reading lock file exists but not used by any process (probably left after crashing)
            var testContext = new FileLockTestContext 
            {
                IsReadScope = false, 
                ReleasedSyncFiles = 2, 
              //  BusySyncFiles = 1 
             };

            var fileLockScope = MockFileLockScope(testContext);

            // Act:         Create another lock file for READING and close locking
            var isLocked = fileLockScope.Lock();

            // Assert:      All the lock file has been deleted
            Assert.IsTrue(isLocked);

            var fileAfterAction = _fileSystem.AllFiles.ToList();
            Assert.AreEqual(0, fileAfterAction.Count);
        }

        [Test]
        public void CanCreateReadingLockerWhenOtherReadingLockersExist()
        {
            // Arrange:     There is already 1 lock file for reading exists (and used by some other process)

            // Act:         Try to create another lock file for READING

            // Assert:      Second lock file successfully created
        }

        [Test]
        public void CannotCreateWritingLockerWhenAnyReadingLockersExist()
        {
            // Arrange:     There is already 1 lock file for reading exists (and used by some other process)

            // Act:         Try to create a lock file for WRITING

            // Assert:      Cannot create Writing lock file
        }

        [Test]
        public void CannotCreateWritingLockerWhenAnyWritingLockersExist()
        {
            // Arrange:     There is already 1 lock file for writing exists (and used by some other process)

            // Act:         Try to create a lock file for WRITING

            // Assert:      Cannot create Writing lock file
        }

        [Test]
        public void CannotCreateReadingLockerWhenAnyWritingLockersExist()
        {
            // Arrange:     There is already 1 lock file for reading exists (and used by some other process)

            // Act:         Try to create a lock file for WRITING

            // Assert:      Cannot create Writing lock file
        }

        [Test]
        public void DeleteReadingLockerAfterUnlocking()
        {
            // Arrange:     There is already 1 lock file for reading exists (and used by some other process)

            // Act:         Unlocking

            // Assert:      Lock file deleted
        }


        //[Test]
        //public async Task FailsToCreateReadLockAsync()
        //{
        //    var fileLocScope = new FileLockScopeWrapper(true);

        //    // TODO: How to deal with IO exception
        //}


        //[Test]
        //public async Task FailsToCreateReadLockAsync()
        //{
        //    var fileServiceMock = new Mock<IFileService>();

        //    fileServiceMock
        //        .Setup(x => x.Create(It.IsAny<string>()))
        //        .Returns<string>(x =>
        //        {
        //            // Mock memory stream instead
        //            // TODO: Consider changing FileStrewam to Stream
        //            return new MockFileStream();
        //        });

        //    var fileLockScope = new FileLockScopeWrapper(true);

        //    Assert.Throws<IOException>(() => fileLockScope.Lock());
        //}

        public class FileLockScopeWrapper : FileLockScope
        {
            private readonly bool _throwIoException;

            public FileLockScopeWrapper(bool isReadScope, string syncFile, IFileService fileService)
                : base(isReadScope, syncFile, fileService)
            {
            }

            public FileLockScopeWrapper(bool throwIoException)
            {
                _throwIoException = throwIoException;
            }

            //protected override Stream CreateLockStream()
            //{
            //    if (_throwIoException)
            //    {
            //        throw new IOException();
            //    }

            //    // TODO: Implement expected behavior here
            //    return base.CreateSyncStream();
            //}
        }
    }
}
