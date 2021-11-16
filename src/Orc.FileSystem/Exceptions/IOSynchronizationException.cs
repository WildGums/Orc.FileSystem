// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOSynchronizationException.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    // ReSharper disable once InconsistentNaming
    public class IOSynchronizationException : Exception
    {
        #region Constructors
        public IOSynchronizationException()
        {

        }

        public IOSynchronizationException(string message)
            : base(message)
        {
        }

        public IOSynchronizationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public IOSynchronizationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
        #endregion
    }
}
