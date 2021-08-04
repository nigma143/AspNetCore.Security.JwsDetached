using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.Security.JwsDetached.IO
{
    class MemoryBufferingReadStream : System.IO.Stream
    {
        private readonly System.IO.Stream _source;
        private readonly MemoryStream _destination;

        private readonly long? _bufferLimit;

        private bool _completelyBuffered = false;

        public MemoryBufferingReadStream(System.IO.Stream source, Func<MemoryStream> destinationFn, 
            long? bufferLimit = null)
        {
            _source = source;
            _destination = destinationFn();
            
            _bufferLimit = bufferLimit;
        }

        public override void Flush()
        {
            _destination.Flush();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        { 
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            if (_completelyBuffered)
            {
                return await _destination.ReadAsync(buffer, cancellationToken);
            }

            var read = await _source.ReadAsync(buffer, cancellationToken);
            
            if (read > 0)
            {
                if (_bufferLimit.HasValue && _bufferLimit - read < _destination.Length)
                {
                    throw new ReadBufferLimitException("Buffer limit exceeded");
                }

                _destination.Write(buffer.Span);
            }
            else
            {
                _completelyBuffered = true;
            }

            return read;
        }
        
        public override int Read(Span<byte> buffer)
        {
            if (_completelyBuffered)
            {
                return _destination.Read(buffer);
            }

            var read = _source.Read(buffer);

            if (read > 0)
            {
                if (_bufferLimit.HasValue && _bufferLimit - read < _destination.Length)
                {
                    throw new ReadBufferLimitException("Buffer limit exceeded");
                }

                _destination.Write(buffer);
            }
            else
            {
                _completelyBuffered = true;
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!_completelyBuffered)
            {
                throw new NotSupportedException("The content has not been fully buffered yet");
            }

            return _destination.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _destination.Length;

        public override long Position
        {
            get => _destination.Position;
            set => _destination.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _destination.Dispose();
            }
        }
    }
}
