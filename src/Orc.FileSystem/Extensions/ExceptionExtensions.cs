// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExceptionExtensions.cs" company="WildGums">
//   Copyright (c) 2008 - 2017 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;

    public static class ExceptionExtensions
    {
        public static int GetHResult(this Exception exception)
        {
            return exception.HResult;
        }
    }
}
