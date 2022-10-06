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
