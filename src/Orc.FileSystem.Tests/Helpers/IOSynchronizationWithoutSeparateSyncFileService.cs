﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOSynchronizationWithoutSepatateSyncFileService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem.Tests
{
    public class IOSynchronizationWithoutSeparateSyncFileService : IOSynchronizationService
    {
        #region Constructors
        public IOSynchronizationWithoutSeparateSyncFileService(IFileService fileService, IDirectoryService directoryService)
            : base(fileService, directoryService)
        {
        }
        #endregion

        protected override string ResolveObservedFileName(string path)
        {
            return path;
        }
    }
}