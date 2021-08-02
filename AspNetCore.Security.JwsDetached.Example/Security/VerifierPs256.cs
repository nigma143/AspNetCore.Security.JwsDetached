using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using JwsDetachedStreaming;

namespace AspNetCore.Security.JwsDetached.Example.Security
{
    class VerifierPs256 : Verifier
    {
        private readonly HashAlgorithm _hashAlgorithm = SHA256.Create();

        private readonly X509Certificate2 _certificate;

        public VerifierPs256(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public override ValueTask WriteInputAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
            {
                _hashAlgorithm.TransformBlock(array.Array!, array.Offset, array.Count, null, 0);
            }
            else
            {
                byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                buffer.Span.CopyTo(sharedBuffer);
                _hashAlgorithm.TransformBlock(sharedBuffer, 0, buffer.Length, null, 0);
            }

            return new ValueTask();
        }

        public override Task<bool> VerifyAsync(ReadOnlyMemory<byte> signature, CancellationToken cancellationToken = default)
        {
            _hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            var hash = _hashAlgorithm.Hash;

            using var privateKey = _certificate.GetRSAPrivateKey();
            return Task.FromResult(privateKey.VerifyHash(hash, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
        }

        protected override void Dispose(bool disposing)
        {
            _hashAlgorithm.Dispose();
        }
    }
}
