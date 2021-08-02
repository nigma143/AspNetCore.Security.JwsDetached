using System;
using System.Security.Cryptography.X509Certificates;
using JwsDetachedStreaming;
using Newtonsoft.Json.Linq;

namespace AspNetCore.Security.JwsDetached.Example.Security
{
    class VerifierFactory : IVerifierFactory
    {
        private readonly X509Certificate2 _certificate;

        public VerifierFactory()
        {
            _certificate = new X509Certificate2("Provision/cert.pfx.test", "123456");
        }

        public Verifier Create(JObject header)
        {
            return header.GetValue("alg").ToString() switch
            {
                "PS256" => new VerifierPs256(_certificate),
                _ => throw new NotSupportedException("Signature algorithm not supported")
            };
        }
    }
}
