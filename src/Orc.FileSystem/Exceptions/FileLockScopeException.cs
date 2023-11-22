namespace Orc.FileSystem;

using System;

[Serializable]
public class FileLockScopeException : Exception
{
    public FileLockScopeException()
    {

    }

    public FileLockScopeException(string message)
        : base(message)
    {
    }

    public FileLockScopeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
