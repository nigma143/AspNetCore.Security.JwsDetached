using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace AspNetCore.Security.JwsDetached.Example.Security
{
    class SignContextSelector : ISignContextSelector
    {
        public SignContext? Select(HttpContext context)
        {
            return new SignContext("PS256", new JObject(), new SignerResolver());
        }
    }
}
