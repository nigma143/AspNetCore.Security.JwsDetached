using System;
using System.Threading.Tasks;
using AspNetCore.Security.JwsDetached.IO;
using JwsDetachedStreaming.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;

namespace AspNetCore.Security.JwsDetached
{
    public static class Buffering
    {
        private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        public static IAsyncDisposable EnableRequestBufferingIfRequired(HttpRequest request, BufferingOptions options)
        {
            switch (options.RequestBufferingType)
            {
                case BufferingType.Disabled:
                    return new DummyDispose();

                case null:
                case BufferingType.File:
                    return EnableRequestFileBuffering(request, options.RequestFileBufferingOptions);

                case BufferingType.Memory:
                    return EnableRequestMemoryBuffering(request, options.RequestMemoryBufferingOptions);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IAsyncDisposable EnableResponseBufferingIfRequired(HttpResponse response, BufferingOptions options)
        {
            switch (options.ResponseBufferingType)
            {
                case BufferingType.Disabled:
                    return new DummyDispose();

                case null:
                case BufferingType.File:
                    return EnableResponseFileBuffering(response, options.ResponseFileBufferingOptions);

                case BufferingType.Memory:
                    return EnableResponseMemoryBuffering(response, options.ResponseMemoryBufferingOptions);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IAsyncDisposable EnableRequestFileBuffering(HttpRequest request, FileBufferingOptions? options = null)
        {
            if (IsRequestBuffered(request))
            {
                return new DummyDispose();
            }

            const int defaultBufferThreshold = 1024 * 30;

            var bufferThreshold = options?.BufferThreshold ?? defaultBufferThreshold;
            var bufferLimit = options?.BufferLimit;
            var tempDirectory = options?.TmpFileDirectory ?? AspNetCoreTempDirectory.TempDirectory;

            if (request.ContentLength.HasValue && bufferLimit.HasValue &&
                request.ContentLength > bufferLimit)
            {
                throw new ReadBufferLimitException("Content length body too large");
            }

            var originRequestStream = request.Body;

            var fileStream = new FileBufferingReadStream(
                originRequestStream, bufferThreshold, bufferLimit, () => tempDirectory);
            request.Body = fileStream;

            request.HttpContext.Features.Set(
                new RequestBufferingFeature(BufferingType.File));

            return new ActionAtDispose(
                async () =>
                {
                    request.Body = originRequestStream;

                    await fileStream.DisposeAsync();
                });
        }

        public static IAsyncDisposable EnableRequestMemoryBuffering(HttpRequest request, MemoryBufferingOptions? options = null)
        {
            if (IsRequestBuffered(request))
            {
                return new DummyDispose();
            }

            var bufferLimit = options?.BufferLimit;

            if (request.ContentLength.HasValue && bufferLimit.HasValue &&
                request.ContentLength > bufferLimit)
            {
                throw new ReadBufferLimitException("Content length body too large");
            }

            var originRequestStream = request.Body;

            var memoryStream = new MemoryBufferingReadStream(
                originRequestStream, () => MemoryStreamManager.GetStream(), bufferLimit);
            request.Body = memoryStream;

            request.HttpContext.Features.Set(
                new RequestBufferingFeature(BufferingType.Memory));

            return new ActionAtDispose(
                async () =>
                {
                    request.Body = originRequestStream;

                    await memoryStream.DisposeAsync();
                });
        }

        private static IAsyncDisposable EnableResponseFileBuffering(HttpResponse response, FileBufferingOptions? options = null)
        {
            if (IsResponseBuffered(response))
            {
                return new DummyDispose();
            }

            const int defaultBufferThreshold = 1024 * 30;

            var bufferThreshold = options?.BufferThreshold ?? defaultBufferThreshold;
            var bufferLimit = options?.BufferLimit;
            var tempDirectory = options?.TmpFileDirectory ?? AspNetCoreTempDirectory.TempDirectory;

            var originResponseStream = response.Body;

            var fileStream = new FileBufferingWriteStream(
                bufferThreshold, bufferLimit, () => tempDirectory);
            response.Body = fileStream;

            response.HttpContext.Features.Set(
                new ResponseBufferingFeature(BufferingType.File));

            return new ActionAtDispose(
                async () =>
                {
                    var responseBody = (FileBufferingWriteStream)response.Body;

                    if (!responseBody.Disposed)
                    {
                        await responseBody.DrainBufferAsync(originResponseStream);
                        await responseBody.DisposeAsync();
                    }

                    response.Body = originResponseStream;
                });
        }

        public static IAsyncDisposable EnableResponseMemoryBuffering(HttpResponse response, MemoryBufferingOptions? options = null)
        {
            if (IsResponseBuffered(response))
            {
                return new DummyDispose();
            }

            var bufferLimit = options?.BufferLimit;

            var originResponseStream = response.Body;

            var memoryStream = new MemoryBufferingWriteStream(
                () => MemoryStreamManager.GetStream(), bufferLimit);
            response.Body = memoryStream;

            response.HttpContext.Features.Set(
                new ResponseBufferingFeature(BufferingType.Memory));

            return new ActionAtDispose(
                async () =>
                {
                    var responseBody = response.Body;

                    responseBody.Position = 0;
                    await responseBody.CopyToAsync(originResponseStream);
                    await responseBody.DisposeAsync();

                    response.Body = originResponseStream;
                });
        }

        public static IAsyncDisposable SlidingWriteStream(HttpResponse response, System.IO.Stream stream)
        {
            var originStream = response.Body;

            var multiStream = new MultiWriteStream(
                stream, originStream);

            response.Body = multiStream;

            return new ActionAtDispose(
                async () =>
                {
                    await multiStream.DisposeAsync();

                    response.Body = originStream;
                });
        }

        public static bool IsRequestBuffered(HttpRequest request)
        {
            return request.HttpContext.Features.Get<RequestBufferingFeature>() != null;
        }

        public static BufferingType GetRequestBufferedType(HttpRequest request)
        {
            var feature = request.HttpContext.Features.Get<RequestBufferingFeature>();
            if (feature == null)
            {
                return BufferingType.Disabled;
            }

            return feature.Type;
        }

        public static bool IsResponseBuffered(HttpResponse response)
        {
            return response.HttpContext.Features.Get<ResponseBufferingFeature>() != null;
        }

        public static BufferingType GetResponseBufferedType(HttpResponse response)
        {
            var feature = response.HttpContext.Features.Get<ResponseBufferingFeature>();
            if (feature == null)
            {
                return BufferingType.Disabled;
            }

            return feature.Type;
        }
    }

    class DummyDispose : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            return default;
        }
    }

    class ActionAtDispose : IAsyncDisposable
    {
        private readonly Func<Task> _action;

        public ActionAtDispose(Func<Task> action)
        {
            _action = action;
        }

        public async ValueTask DisposeAsync()
        {
            await _action();
        }
    }
}
