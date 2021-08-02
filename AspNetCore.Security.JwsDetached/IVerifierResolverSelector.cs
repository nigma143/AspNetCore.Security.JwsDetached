using JwsDetachedStreaming;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Security.JwsDetached
{
    public interface IVerifierResolverSelector
    {
        IVerifierFactory? Select(HttpContext context);
    }
}
