namespace AspNetCore.Security.JwsDetached
{
    class RequestBufferingFeature
    {
        public RequestBufferingFeature(BufferingType type)
        {
            Type = type;
        }

        public BufferingType Type { get; }
    }
}
