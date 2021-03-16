using System;
using System.Threading.Tasks;
using JwsDetachedStreaming;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

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
                var jwsHeader = handler.Read(jwsDetached, verifierResolver, payloadStream);

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
