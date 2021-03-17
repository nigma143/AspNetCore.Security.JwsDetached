using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
                            services.AddJwsDetached<VerifierResolverSelector, SignContextSelector>();
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
            
            host.GetTestServer().AllowSynchronousIO = true;
            
            var handler = new JwsDetachedHandler();
            var jwsDetached = handler.Write(new JObject(), "PS256", new SignerResolver(),
                new MemoryStream(Encoding.UTF8.GetBytes("Request body")));

            var client = host.GetTestClient();
            client.DefaultRequestHeaders.Add("x-jws-signature", jwsDetached);

            var response = await client.PostAsync("/", new StringContent("Request body"));
            response.EnsureSuccessStatusCode();

            Assert.IsTrue(response.Headers.Contains("x-jws-signature"));
            
            var jwsHeaders = handler.Read(String.Join("", response.Headers.GetValues("x-jws-signature")), 
                new VerifierResolver(),
                await response.Content.ReadAsStreamAsync());

            Assert.IsNotNull(jwsHeaders);

            var contentRaw = await response.Content.ReadAsByteArrayAsync();

            Assert.IsTrue(contentRaw.SequenceEqual(
                Encoding.UTF8.GetBytes("Response body")));
        }
    }

    class VerifierResolverSelector : IVerifierResolverSelector
    {
        public IVerifierResolver? Select(HttpContext context)
        {
            return new VerifierResolver();
        }
    }

    class SignContextSelector : ISignContextSelector
    {
        public SignContext? Select(HttpContext context)
        {
            return new SignContext("PS256", new JObject(), new SignerResolver());
        }
    }

    class SignerResolver : ISignerResolver
    {
        private readonly X509Certificate2 _certificate;

        public SignerResolver()
        {
            _certificate = new X509Certificate2("Provision/cert.pfx.test", "123456");
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

    class VerifierResolver : IVerifierResolver
    {
        private readonly X509Certificate2 _certificate;

        public VerifierResolver()
        {
            _certificate = new X509Certificate2("Provision/cert.pfx.test", "123456");
        }

        public IVerifier Resolve(JObject header)
        {
            return header.GetValue("alg").ToString() switch
            {
                "PS256" => new VerifierPs256(_certificate),
                _ => throw new NotSupportedException("Signature algorithm not supported")
            };
        }
    }
    
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
