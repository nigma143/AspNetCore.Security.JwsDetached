using System;
using System.Security.Cryptography.X509Certificates;
using JwsDetachedStreaming;
using Newtonsoft.Json.Linq;

namespace AspNetCore.Security.JwsDetached.Example.Security
{
    class SignerResolver : ISignerResolver
    {
        private readonly X509Certificate2 _certificate;

        public SignerResolver()
        {
            _certificate = new X509Certificate2("cert.pfx.test", "123456");
        }

        public ISigner Resolve(JObject header)
        {
            return header.GetValue("alg").ToString() switch
            {
                "PS256" => new SignerPs256(_certificate),
                _ => throw new NotSupportedException("Signature algorithm not supported")
            };
        }
    }
}
