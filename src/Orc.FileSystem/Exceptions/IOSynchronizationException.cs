namespace Orc.FileSystem;

using System;

[Serializable]
public class IOSynchronizationException : Exception
{
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
}
