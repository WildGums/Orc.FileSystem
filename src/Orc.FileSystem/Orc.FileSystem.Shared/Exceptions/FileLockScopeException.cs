// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileLockScopeException.cs" company="WildGums">
//   Copyright (c) 2008 - 2017 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;

    public class FileLockScopeException : Exception
    {
        #region Constructors
        public FileLockScopeException(string message)
            : base(message)
        {
        }

        public FileLockScopeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion
    }
}