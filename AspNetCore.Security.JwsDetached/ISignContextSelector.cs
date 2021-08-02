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
        public JObject Header { get; }

        public ISignerFactory SignerFactory { get; }

        public SignContext(JObject header, ISignerFactory signerFactory)
        {
            Header = header;
            SignerFactory = signerFactory;
        }
    }
}
