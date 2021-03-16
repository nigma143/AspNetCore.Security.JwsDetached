using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using JwsDetachedStreaming;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetCore.Security.JwsDetached.Test
{
    [TestClass]
    public class JwsDetachedMiddlewareTest
    {
        [TestMethod]
        public void TestMethod1()
        {/*
            using var host = await new HostBuilder()
                                   .ConfigureWebHost(webBuilder =>
                                   {
                                       webBuilder
                                           .UseTestServer()
                                           .ConfigureServices(services =>
                                           {
                                               services.Configure<JwsDetachedOptions>(
                                                   options =>
                                                   {
                                                       options.Signing = new SigningOptions()
                                                       {
                                                           Algorithm = "HS256",
                                                           KeySource = SigningKeySource.SymmetricKeyFromFile,
                                                           FileOptions = new SigningKeySourceFileOptions()
                                                           {
                                                               FileName = hs256KeyFileName
                                                           }
                                                       };
                                                   });
                                           })
                                           .Configure(app =>
                                           {
                                               app
                                                   .UseMiddleware<JwsDetachedMiddleware>()
                                                   .Use(async (context, next) =>
                                                   {
                                                       var jwsDetached = context.Request.Headers["X-JWS-Signature"]
                                                                                .Single();

                                                       var jwsDetachedParts = jwsDetached.Split('.');

                                                       var payloadMs = new MemoryStream();

                                                       context.Request.Body.Position = 0;
                                                       await context.Request.Body.CopyToAsync(payloadMs);

                                                       var encodedPayload = Base64UrlEncoder.Encode(payloadMs.ToArray());

                                                       var jws = string.Concat(jwsDetachedParts[0], ".", encodedPayload, ".", jwsDetachedParts[2]);

                                                       var handler = new JsonWebSignatureHandler();

                                                       handler.Validate(jws, new SymmetricSecurityKey(
                                                                            Convert.FromBase64String(
                                                                                await File.ReadAllTextAsync(hs256KeyFileName, Encoding.UTF8))));

                                                       await next.Invoke();
                                                   })
                                                   .Run(async context =>
                                                   {
                                                       context.Response.StatusCode = 200;
                                                       await context.Response.Body.WriteAsync(
                                                           Encoding.UTF8.GetBytes("Response body"));
                                                   });
                                           });
                                   })
                                   .StartAsync();

            var response = await host.GetTestClient()
                                     .PostAsync("/", new ByteArrayContent(Encoding.UTF8.GetBytes("Request body")));
            response.EnsureSuccessStatusCode();

            var contentRaw = await response.Content.ReadAsByteArrayAsync();

            Assert.IsTrue(contentRaw.SequenceEqual(
                              Encoding.UTF8.GetBytes("Response body")));*/
        }
    }
}
