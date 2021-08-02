using JwsDetachedStreaming;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Security.JwsDetached.Example.Security
{
    class VerifierResolverSelector : IVerifierResolverSelector
    {
        public IVerifierFactory? Select(HttpContext context)
        {
            return new VerifierFactory();
        }
    }
}
