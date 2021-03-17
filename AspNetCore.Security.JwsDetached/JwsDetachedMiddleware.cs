using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JwsDetachedStreaming;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace AspNetCore.Security.JwsDetached
{
    public class JwsDetachedMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly JwsDetachedOptions _options;
        private readonly IVerifierResolverSelector _verifierResolverSelector;
        private readonly ISignContextSelector _signContextSelector;

        public JwsDetachedMiddleware(RequestDelegate next, IOptions<JwsDetachedOptions> options,
            IVerifierResolverSelector verifierResolverSelector, ISignContextSelector signContextSelector)
        {
            _next = next;
            _signContextSelector = signContextSelector;
            _verifierResolverSelector = verifierResolverSelector;
            _options = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await _next.Invoke(context);
            }

            IAsyncDisposable? requestBuffering = null;
            IAsyncDisposable? responseBuffering = null;
            try
            {
                var verifierResolver = _verifierResolverSelector.Select(context);
                if (verifierResolver != null)
                {
                    requestBuffering = Buffering.EnableRequestBufferingIfRequired(context.Request, _options.Buffering);

                    VerifyRequest(context.Request, verifierResolver);
                }

                var signContext = _signContextSelector.Select(context);
                if (signContext != null)
                {
                    responseBuffering = Buffering.EnableResponseBufferingIfRequired(context.Response, _options.Buffering);
                }

                await _next.Invoke(context);

                if (signContext != null)
                {
                    SignResponse(context.Response, signContext);
                }
            }
            finally
            {
                if (requestBuffering != null)
                {
                    await requestBuffering.DisposeAsync();
                }

                if (responseBuffering != null)
                {
                    await responseBuffering.DisposeAsync();
                }
            }
        }

        private void VerifyRequest(HttpRequest request, IVerifierResolver verifierResolver)
        {
            if (!request.Headers.TryGetValue(
                _options.HeaderName, out var jwsDetachedHeader))
            {
                throw new JwsDetachedException(JwsDetachedType.HeaderNotFound);
            }

            var jwsDetached = jwsDetachedHeader.ToString();

            var payloadStream = request.Body;

            var beforePayloadPosition = payloadStream.Position;
            try
            {
                payloadStream.Position = 0;

                var handler = new JwsDetachedHandler();

                JObject? jwsHeader;
                try
                {
                    jwsHeader = handler.Read(jwsDetached, verifierResolver, payloadStream);
                }
                catch (Exception ex)
                {
                    throw new JwsDetachedException(JwsDetachedType.ReadError, ex);
                }

                if (jwsHeader == null)
                {
                    throw new JwsDetachedException(JwsDetachedType.InvalidSignature);
                }
            }
            finally
            {
                payloadStream.Position = beforePayloadPosition;
            }
        }

        public JObject? Read(string jwsDetached, IVerifierResolver verifierResolver, Stream payloadStream)
        {
            var parts = jwsDetached.Split('.');
            if (parts.Length != 3)
            {
                throw new FormatException("Expected three segments");
            }

            var encodedHeaderBytes = Encoding.UTF8.GetBytes(parts[0]);

            var header = JObject.Parse(
                Encoding.UTF8.GetString(
                    Base64UrlEncoder.Decode(encodedHeaderBytes)));
            // part[1] is detached payload
            var signature = Base64UrlEncoder.DecodeFromString(parts[2]);

            using var encodedPayloadStream = new CryptoStream(
                payloadStream,
                new ToBase64UrlTransform(),
                CryptoStreamMode.Read,
                leaveOpen: true);

            var verifier = verifierResolver.Resolve(header);

            var verify = verifier.Verify(
                new CompositeReadStream(
                    new[] { encodedHeaderBytes, new byte[] { 0xE2 } }, //E2 - dot byte
                    encodedPayloadStream,
                    leaveOpen: true),
                signature);

            if (!verify)
            {
                return null;
            }

            return header;
        }

        private void SignResponse(HttpResponse response, SignContext signContext)
        {
            var payloadStream = response.Body;

            var beforePayloadPosition = payloadStream.Position;
            try
            {
                payloadStream.Position = 0;

                var handler = new JwsDetachedHandler();
                var jwsDetached = handler.Write(
                    signContext.Header, signContext.Algorithm, signContext.SignerResolver, payloadStream);

                response.Headers[_options.HeaderName] = jwsDetached;
            }
            finally
            {
                payloadStream.Position = beforePayloadPosition;
            }
        }
    }
}
