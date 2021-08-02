using System;
using System.Security.Cryptography.X509Certificates;
using JwsDetachedStreaming;
using Newtonsoft.Json.Linq;

namespace AspNetCore.Security.JwsDetached.Example.Security
{
    class SignerFactory : ISignerFactory
    {
        private readonly X509Certificate2 _certificate;

        public SignerFactory()
        {
            _certificate = new X509Certificate2("Provision/cert.pfx.test", "123456");
        }

        public Signer Create(JObject header)
        {
            return header.GetValue("alg").ToString() switch
            {
                "PS256" => new SignerPs256(_certificate),
                _ => throw new NotSupportedException("Signature algorithm not supported")
            };
        }
    }
}
