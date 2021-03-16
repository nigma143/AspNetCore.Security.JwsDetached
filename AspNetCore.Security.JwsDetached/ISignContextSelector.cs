using JwsDetachedStreaming;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace AspNetCore.Security.JwsDetached
{
    public interface ISignContextSelector
    {
        SignContext? Select(HttpContext context);
    }

    public class SignContext 
    {
        public string Algorithm { get; }

        public JObject Header { get; }

        public ISignerResolver SignerResolver { get; }

        public SignContext(string algorithm, JObject header, ISignerResolver signerResolver)
        {
            Algorithm = algorithm;
            Header = header;
            SignerResolver = signerResolver;
        }
    }
}
