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
    using System.Threading;

    public static class FileInfoExtensions
    {
        private const int TimerTickIntervalInMilliseconds = 50;

        #region Methods
        public static async Task EnsureFilesNotBusyAsync(this IEnumerable<FileInfo> files)
        {
            var tcs = new TaskCompletionSource<object>();

            Timer timer = null;

            var handler = new TimerCallback(x =>
            {
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
                        timer.Change(TimerTickIntervalInMilliseconds, Timeout.Infinite);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                        return;
                    }
                }

                tcs.TrySetResult(null);
            });

            using (timer = new Timer(handler, null, TimerTickIntervalInMilliseconds, Timeout.Infinite))
            {
                await tcs.Task;
            }
        }
        #endregion
    }
}