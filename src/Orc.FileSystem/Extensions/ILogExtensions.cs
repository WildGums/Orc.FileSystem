namespace Orc.FileSystem;

using System.Diagnostics;
using Microsoft.Extensions.Logging;

internal static class ILogExtensions
{
    private static readonly bool IsDebuggerAttached;

    static ILogExtensions()
    {
        IsDebuggerAttached = Debugger.IsAttached;
    }

    public static void LogDebugIfAttached(this ILogger logger, string message)
    {
        if (IsDebuggerAttached)
        {
            logger.LogDebug(message);
        }
    }
}
