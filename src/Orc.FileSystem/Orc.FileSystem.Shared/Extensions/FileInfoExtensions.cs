// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileInfoExtensions.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using System.Timers;

    public static class FileInfoExtensions
    {
        #region Methods
        public static Task EnsureFilesNotBusyAsync(this IEnumerable<FileInfo> files)
        {
            var tcs = new TaskCompletionSource<object>();

            var timer = new Timer
            {
                Interval = 50
            };

            timer.Elapsed += (sender, args) =>
            {
                timer.Stop();

                foreach (var file in files)
                {
                    try
                    {
                        if (!file.Exists)
                        {
                            continue;
                        }

                        using (file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            // don't do anything
                        }
                    }
                    catch (IOException)
                    {
                        timer.Start();
                        return;
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                        return;
                    }
                }

                tcs.TrySetResult(null);
            };

            timer.Start();

            return tcs.Task;
        }
        #endregion
    }
}