namespace Orc.FileSystem
{
    public class FileLockScopeContext
    {
        public bool? IsReadScope { get; set; }
        public string DirectoryName { get; set; }
        public string FileName { get; set; }
        public bool HasId { get; set; }
    }
}
