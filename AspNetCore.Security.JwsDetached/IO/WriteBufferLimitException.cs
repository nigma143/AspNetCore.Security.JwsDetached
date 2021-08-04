using System;

namespace AspNetCore.Security.JwsDetached.IO
{
    public class WriteBufferLimitException : Exception
    {
        public WriteBufferLimitException(string message)
            : base(message)
        {

        }
    }
}
