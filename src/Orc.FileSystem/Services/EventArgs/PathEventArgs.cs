namespace Orc.FileSystem
{
    using System;
    using Catel;

    public class PathEventArgs : EventArgs
    {
        public PathEventArgs(string path)
        {
            Argument.IsNotNullOrWhitespace(() => path);

            Path = path;
        }

        public string Path { get; private set; }
    }
}
