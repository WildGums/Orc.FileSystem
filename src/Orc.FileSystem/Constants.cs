namespace Orc.FileSystem;

// for full list of error codes see https://msdn.microsoft.com/en-us/library/windows/desktop/ms681382.aspx
public static class SystemErrorCodes
{
    /// <summary>
    /// The process cannot access the file because it is being used by another process.
    /// </summary>
    public const uint ERROR_SHARING_VIOLATION = 0x80070020;
}