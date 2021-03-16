namespace AspNetCore.Security.JwsDetached
{
    public class JwsDetachedOptions
    {
        public string HeaderName { get; set; } = null!;

        public BufferingOptions Buffering = new BufferingOptions();
    }
}
