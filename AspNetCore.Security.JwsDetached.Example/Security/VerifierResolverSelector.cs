using JwsDetachedStreaming;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Security.JwsDetached.Example.Security
{
    class VerifierResolverSelector : IVerifierResolverSelector
    {
        public IVerifierResolver? Select(HttpContext context)
        {
            return new VerifierResolver();
        }
    }
}
