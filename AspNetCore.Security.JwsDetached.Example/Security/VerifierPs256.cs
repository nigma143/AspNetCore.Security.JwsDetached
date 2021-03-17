using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using JwsDetachedStreaming;

namespace AspNetCore.Security.JwsDetached.Example.Security
{
    class VerifierPs256 : IVerifier
    {
        private readonly X509Certificate2 _certificate;

        public VerifierPs256(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public bool Verify(Stream inputStream, byte[] signature)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(inputStream);

            using var privateKey = _certificate.GetRSAPrivateKey();
            return privateKey.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }
    }
}
