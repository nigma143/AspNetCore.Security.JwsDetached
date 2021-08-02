using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace AspNetCore.Security.JwsDetached.Example.Security
{
    class SignContextSelector : ISignContextSelector
    {
        public SignContext? Select(HttpContext context)
        {
            return new SignContext(
                new JObject()
                {
                    {"alg", "PS256"}
                },
                new SignerFactory());
        }
    }
}
