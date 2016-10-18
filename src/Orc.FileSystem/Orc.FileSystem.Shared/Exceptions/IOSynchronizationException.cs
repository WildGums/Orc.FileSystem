// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOSynchronizationException.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;

    public class IOSynchronizationException : Exception
    {
        #region Constructors
        public IOSynchronizationException(string meaasge)
            : base(meaasge)
        {
        }

        public IOSynchronizationException(string meaasge, Exception innerException)
            : base(meaasge, innerException)
        {
        }
        #endregion
    }
}