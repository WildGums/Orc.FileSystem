namespace Orc.FileSystem;

using System;
using System.Runtime.Serialization;

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

    protected FileLockScopeException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {

    }
}
