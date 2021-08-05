namespace AspNetCore.Security.JwsDetached
{
    class ResponseBufferingFeature
    {
        public ResponseBufferingFeature(BufferingType type)
        {
            Type = type;
        }

        public BufferingType Type { get; }
    }
}
