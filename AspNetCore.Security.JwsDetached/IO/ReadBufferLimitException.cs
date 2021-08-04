using System;

namespace AspNetCore.Security.JwsDetached.IO
{
    public class ReadBufferLimitException : Exception
    {
        public ReadBufferLimitException(string message)
            : base(message)
        {

        }
    }
}
