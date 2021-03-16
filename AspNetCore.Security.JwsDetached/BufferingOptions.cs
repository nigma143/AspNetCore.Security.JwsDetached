namespace AspNetCore.Security.JwsDetached
{
    /// <summary>
    /// Buffering options
    /// </summary>
    public class BufferingOptions
    {
        /// <summary>
        /// HttpRequest.Body buffering type
        /// </summary>
        public BufferingType? RequestBufferingType { get; set; }

        /// <summary>
        /// File buffering options
        /// </summary>
        public FileBufferingOptions? RequestFileBufferingOptions { get; set; }

        /// <summary>
        /// HttpResponse.Body buffering
        /// </summary>
        public BufferingType? ResponseBufferingType { get; set; }

        /// <summary>
        /// File buffering options
        /// </summary>
        public FileBufferingOptions? ResponseFileBufferingOptions { get; set; }
    }

    /// <summary>
    /// Buffering type
    /// </summary>
    public enum BufferingType
    {
        /// <summary>
        /// File buffering
        /// </summary>
        File,

        /// <summary>
        /// Memory buffering
        /// </summary>
        Memory
    }
    
    /// <summary>
    /// File buffering options
    /// </summary>
    public class FileBufferingOptions
    {
        /// <summary>
        /// The maximum size in bytes of the in-memory ArrayPool<T> used to buffer the stream. Larger request bodies are written to disk.
        /// </summary>
        public int? BufferThreshold { get; set; }

        /// <summary>
        /// The maximum size in bytes of the request body. An attempt to read beyond this limit will cause an IOException.
        /// </summary>
        public long? BufferLimit { get; set; }

        /// <summary>
        /// Temporary files directory for larger requests are written to the location named in the ASPNETCORE_TEMP environment variable, if any. If that environment variable is not defined, these files are written to the current user's temporary folder. Files are automatically deleted at the end of their associated requests.
        /// </summary>
        public string? TmpFileDirectory { get; set; }
    }
}
