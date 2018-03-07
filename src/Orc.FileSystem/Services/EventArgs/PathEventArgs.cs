// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PathEventArgs.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


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