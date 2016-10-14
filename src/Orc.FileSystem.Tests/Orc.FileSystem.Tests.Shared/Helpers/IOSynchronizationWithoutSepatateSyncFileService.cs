// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOSynchronizationWithoutSepatateSyncFileService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem.Tests
{
    public class IOSynchronizationWithoutSepatateSyncFileService : IOSynchronizationService
    {
        #region Constructors
        public IOSynchronizationWithoutSepatateSyncFileService(IFileService fileService)
            : base(fileService)
        {
        }
        #endregion

        protected override string ResolveObservedFileName(string path)
        {
            return path;
        }
    }
}