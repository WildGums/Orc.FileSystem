// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExceptionExtensions.cs" company="WildGums">
//   Copyright (c) 2008 - 2017 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Catel.Reflection;

    public static class ExceptionExtensions
    {
        public static int GetHResult(this Exception exception)
        {
#if NET40 || NET45
            var hResult = exception.GetType().GetPropertiesEx(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .First(x => x.Name.Equals("HResult"));

            var value = hResult.GetValue(exception, null);

            return (int)value;
#else
            return exception.HResult;
#endif
        }
    }
}