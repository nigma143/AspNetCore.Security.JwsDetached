using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using JwsDetachedStreaming;

namespace AspNetCore.Security.JwsDetached.Example.Security
{
    class SignerPs256 : ISigner
    {
        private readonly X509Certificate2 _certificate;

        public SignerPs256(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public byte[] Sign(Stream inputStream)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(inputStream);

            using var privateKey = _certificate.GetRSAPrivateKey();
            return privateKey.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }
    }
}
