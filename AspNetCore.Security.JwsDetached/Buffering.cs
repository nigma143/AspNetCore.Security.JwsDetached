﻿using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
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
                case null:
                case BufferingType.File:
                    return EnableRequestFileBuffering(request, options.RequestFileBufferingOptions);

                case BufferingType.Memory:
                    return EnableRequestMemoryBuffering(request);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IAsyncDisposable EnableResponseBufferingIfRequired(HttpResponse response, BufferingOptions options)
        {
            switch (options.ResponseBufferingType)
            {
                case null:
                case BufferingType.Memory:
                    return EnableResponseMemoryBuffering(response);

                case BufferingType.File:
                    throw new NotSupportedException("Response file buffering not supported");
                    //return EnableResponseFileBuffering(response, options.ResponseFileBufferingOptions);
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IAsyncDisposable EnableRequestFileBuffering(HttpRequest request, FileBufferingOptions? options = null)
        {
            var body = request.Body;
            if (body.CanSeek)
            {
                return new DummyDispose();
            }

            const int defaultBufferThreshold = 1024 * 30;

            var bufferThreshold = options?.BufferThreshold ?? defaultBufferThreshold;
            var bufferLimit = options?.BufferLimit;
            var tempDirectory = options?.TmpFileDirectory ?? AspNetCoreTempDirectory.TempDirectory;

            var originRequestStream = request.Body;

            var fileStream = new SuppressFlushExceptionFileBufferingReadStream(body, bufferThreshold, bufferLimit, () => tempDirectory);
            request.Body = fileStream;

            return new ActionAtDispose(
                async () =>
                {
                    request.Body = originRequestStream;

                    await fileStream.DisposeAsync();
                });
        }

        private static IAsyncDisposable EnableRequestMemoryBuffering(HttpRequest request)
        {
            var body = request.Body;
            if (body.CanRead && body.CanSeek)
            {
                return new DummyDispose();
            }

            var originRequestStream = request.Body;

            var memoryStream = MemoryStreamManager.GetStream();
            originRequestStream.CopyToAsync(memoryStream);

            memoryStream.Position = 0;
            request.Body = memoryStream;

            return new ActionAtDispose(
                async () =>
                {
                    request.Body = originRequestStream;

                    await memoryStream.DisposeAsync();
                });
        }

        /*
        private static IAsyncDisposable EnableResponseFileBuffering(HttpResponse response, FileBufferingOptions? options = null)
        {
            var body = response.Body;
            if (body.CanSeek)
            {
                return new DummyDispose();
            }

            const int defaultBufferThreshold = 1024 * 30;

            var bufferThreshold = options?.BufferThreshold ?? defaultBufferThreshold;
            var bufferLimit = options?.BufferLimit;
            var tempDirectory = options?.TmpFileDirectory ?? AspNetCoreTempDirectory.TempDirectory;

            var originResponseStream = response.Body;

            var fileStream = new FileBufferingWriteStream(bufferThreshold, bufferLimit, () => tempDirectory);
            response.Body = fileStream;
            
            return new ActionAtDispose(
                async () =>
                {
                    var responseBody = (FileBufferingWriteStream)response.Body;

                    await responseBody.DrainBufferAsync(originResponseStream);

                    await responseBody.DisposeAsync();

                    response.Body = originResponseStream;
                });
        }
        */
        
        private static IAsyncDisposable EnableResponseMemoryBuffering(HttpResponse response)
        {
            var body = response.Body;
            if (body.CanRead && body.CanSeek)
            {
                return new DummyDispose();
            }

            var originResponseStream = response.Body;

            var memoryStream = MemoryStreamManager.GetStream();
            response.Body = memoryStream;

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

    class SuppressFlushExceptionFileBufferingReadStream : FileBufferingReadStream
    {
        public SuppressFlushExceptionFileBufferingReadStream(Stream inner, int memoryThreshold) : base(inner, memoryThreshold)
        {
        }

        public SuppressFlushExceptionFileBufferingReadStream(Stream inner, int memoryThreshold, long? bufferLimit, Func<string> tempFileDirectoryAccessor) : base(inner, memoryThreshold, bufferLimit, tempFileDirectoryAccessor)
        {
        }

        public SuppressFlushExceptionFileBufferingReadStream(Stream inner, int memoryThreshold, long? bufferLimit, Func<string> tempFileDirectoryAccessor, ArrayPool<byte> bytePool) : base(inner, memoryThreshold, bufferLimit, tempFileDirectoryAccessor, bytePool)
        {
        }

        public SuppressFlushExceptionFileBufferingReadStream(Stream inner, int memoryThreshold, long? bufferLimit, string tempFileDirectory) : base(inner, memoryThreshold, bufferLimit, tempFileDirectory)
        {
        }

        public SuppressFlushExceptionFileBufferingReadStream(Stream inner, int memoryThreshold, long? bufferLimit, string tempFileDirectory, ArrayPool<byte> bytePool) : base(inner, memoryThreshold, bufferLimit, tempFileDirectory, bytePool)
        {
        }

        public override void Flush()
        {
        }
    }
}
