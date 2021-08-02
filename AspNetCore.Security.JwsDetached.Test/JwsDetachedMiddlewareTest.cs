using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.Security.JwsDetached.DependencyInjection;
using JwsDetachedStreaming;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AspNetCore.Security.JwsDetached.Test
{
    [TestClass]
    public class JwsDetachedMiddlewareTest
    {
        [TestMethod]
        public async Task CommonTest()
        {
            using var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddJwsDetached<VerifierResolverSelector, SignContextSelector>(
                                options =>
                                {
                                    options.Buffering.RequestBufferingType = BufferingType.File;
                                    options.Buffering.RequestFileBufferingOptions =
                                        new FileBufferingOptions()
                                        {
                                            BufferThreshold = 1
                                        };
                                    options.Buffering.ResponseBufferingType = BufferingType.File;
                                    options.Buffering.ResponseFileBufferingOptions =
                                        new FileBufferingOptions()
                                        {
                                            BufferThreshold = 1
                                        };
                                });
                            services.AddControllers();
                        })
                        .Configure(app =>
                        {
                            app
                                .UseHttpJwsDetached()
                                .UseRouting()
                                .UseEndpoints(builder =>
                                    builder.Map("/", context => context.Response.WriteAsync("Response body")));
                        });
                })
                .StartAsync();
            
            await using var payload = new MemoryStream(Encoding.UTF8.GetBytes("Request body"));
            await using var output = new MemoryStream();

            await using var writer = await JwsDetachedWriter.CreateAsync(output, "PS256", new SignerFactory());
            payload.Position = 0;
            await payload.CopyToAsync(writer.Payload);
            await writer.Finish();

            var jwsDetached = Encoding.ASCII.GetString(output.ToArray());

            var client = host.GetTestClient();
            client.DefaultRequestHeaders.Add("x-jws-signature", jwsDetached);

            var response = await client.PostAsync("/", new StringContent("Request body"));
            response.EnsureSuccessStatusCode();

            Assert.IsTrue(response.Headers.Contains("x-jws-signature"));

            await using var reader = await JwsDetachedReader.CreateAsync(
                Encoding.ASCII.GetBytes(
                    String.Join("", response.Headers.GetValues("x-jws-signature"))),
                new VerifierFactory());
            await response.Content.CopyToAsync(reader.Payload);

            var jwsHeaders = await reader.ReadAsync();

            Assert.IsNotNull(jwsHeaders);

            var contentRaw = await response.Content.ReadAsByteArrayAsync();

            Assert.IsTrue(contentRaw.SequenceEqual(
                Encoding.UTF8.GetBytes("Response body")));
        }
    }

    class VerifierResolverSelector : IVerifierResolverSelector
    {
        public IVerifierFactory? Select(HttpContext context)
        {
            return new VerifierFactory();
        }
    }

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

    class SignerPs256 : Signer
    {
        private readonly HashAlgorithm _hashAlgorithm = SHA256.Create();

        private readonly X509Certificate2 _certificate;

        public SignerPs256(X509Certificate2 certificate)
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

        public override Task<byte[]> GetSignatureAsync(CancellationToken cancellationToken = default)
        {
            _hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            var hash = _hashAlgorithm.Hash;

            using var privateKey = _certificate.GetRSAPrivateKey();
            return Task.FromResult(privateKey.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
        }

        protected override void Dispose(bool disposing)
        {
            _hashAlgorithm.Dispose();
        }
    }

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
