namespace AspNetCore.Security.JwsDetached
{
    public class JwsDetachedOptions
    {
        public string HeaderName { get; set; } = "x-jws-signature";

        public BufferingOptions Buffering = new BufferingOptions();
    }
}
