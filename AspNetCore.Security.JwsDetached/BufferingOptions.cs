namespace AspNetCore.Security.JwsDetached
{
    public class BufferingOptions
    {
        public BufferingType? RequestBufferingType { get; set; }

        public FileBufferingOptions? RequestFileBufferingOptions { get; set; }

        public MemoryBufferingOptions? RequestMemoryBufferingOptions { get; set; }

        public BufferingType? ResponseBufferingType { get; set; }

        public FileBufferingOptions? ResponseFileBufferingOptions { get; set; }

        public MemoryBufferingOptions? ResponseMemoryBufferingOptions { get; set; }
    }

    public enum BufferingType
    {
        Disabled,

        File,

        Memory
    }

    public class FileBufferingOptions
    {
        public int? BufferThreshold { get; set; }

        public long? BufferLimit { get; set; }

        public string? TmpFileDirectory { get; set; }
    }

    public class MemoryBufferingOptions
    {
        public long? BufferLimit { get; set; }
    }
}