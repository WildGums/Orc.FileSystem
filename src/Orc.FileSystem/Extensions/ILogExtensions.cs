namespace Orc.FileSystem
{
    using System.Diagnostics;
    using Catel.Logging;

    // ReSharper disable once InconsistentNaming
    internal static class ILogExtensions
    {
        #region Constants
        private static readonly bool IsDebuggerAttached = false;
        #endregion

        #region Constructors
        static ILogExtensions()
        {
            IsDebuggerAttached = Debugger.IsAttached;
        }
        #endregion

        #region Methods
        public static void DebugIfAttached(this ILog log, string message)
        {
            if (IsDebuggerAttached)
            {
                log.Debug(message);
            }
        }
        #endregion
    }
}
