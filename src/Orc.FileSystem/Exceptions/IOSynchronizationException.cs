namespace Orc.FileSystem
{
    using System;
    using System.Runtime.Serialization;

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

        public IOSynchronizationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
