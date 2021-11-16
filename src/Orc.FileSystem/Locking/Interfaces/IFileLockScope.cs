namespace Orc.FileSystem
{
    using System;

    public interface IFileLockScope : IDisposable
    {
        bool NotifyOnRelease { get; set; }
        void WriteDummyContent();
        bool Lock();
        void Unlock();
    }
}
