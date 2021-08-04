using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.Security.JwsDetached.IO;
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

                    await VerifyRequestAsync(context.Request, verifierResolver);
                }

                var signContext = _signContextSelector.Select(context);
                if (signContext != null)
                {
                    responseBuffering = Buffering.EnableResponseBufferingIfRequired(context.Response, _options.Buffering);

                    await NextInvokeAndSignResponseAsync(context, signContext);
                }
                else
                {
                    await _next.Invoke(context);
                }
            }
            catch (ReadBufferLimitException)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsync("Request body too large");
            }
            catch (WriteBufferLimitException)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                await context.Response.WriteAsync("Gateway response body too large");
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

        private async Task VerifyRequestAsync(HttpRequest request, IVerifierFactory verifierFactory)
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
                await using var reader = await JwsDetachedReader.CreateAsync(
                    Encoding.ASCII.GetBytes(jwsDetached),
                    verifierFactory);
                await payloadStream.CopyToAsync(reader.Payload);

                JObject? jwsHeader;
                try
                {
                    jwsHeader = await reader.ReadAsync();
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

        private async Task NextInvokeAndSignResponseAsync(HttpContext context, SignContext signContext)
        {
            await using var jwsDetached = new MemoryStream();

            await using var writer = await JwsDetachedWriter.CreateAsync(
                jwsDetached,
                signContext.Header,
                signContext.SignerFactory);

            await using (Buffering.SlidingWriteStream(context.Response, writer.Payload))
            {
                await _next.Invoke(context);
            }

            await writer.Finish();
            context.Response.Headers[_options.HeaderName] = Encoding.ASCII.GetString(jwsDetached.ToArray());
        }
    }
}
