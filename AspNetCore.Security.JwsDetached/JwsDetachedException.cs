using System;

namespace AspNetCore.Security.JwsDetached
{
    public enum JwsDetachedType
    {
        HeaderNotFound,
        InvalidSignature,
        ReadError
    }

    public class JwsDetachedException : Exception
    {
        public JwsDetachedType Type { get; }

        public override string Message => Type switch
        {
            JwsDetachedType.HeaderNotFound => "Header not found",
            JwsDetachedType.InvalidSignature => "Invalid signature",
            JwsDetachedType.ReadError => "Read error. See inner exception",
            _ => throw new ArgumentOutOfRangeException()
        };

        public JwsDetachedException(JwsDetachedType type, Exception? innerException = null)
            : base(null, innerException)
        {
            Type = type;
        }
    }
}
