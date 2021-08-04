using System;
using System.IO;

namespace AspNetCore.Security.JwsDetached.IO
{
    class MemoryBufferingWriteStream : System.IO.Stream
    {
        private readonly MemoryStream _destination;
        private readonly long? _bufferLimit;

        public MemoryBufferingWriteStream(Func<MemoryStream> destinationFn,
            long? bufferLimit = null)
        {
            _destination = destinationFn();

            _bufferLimit = bufferLimit;
        }

        public override void Flush()
        {
            _destination.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _destination.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _destination.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _destination.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_bufferLimit.HasValue && _bufferLimit - Length < count)
            {
                throw new WriteBufferLimitException("Buffer limit exceeded.");
            }

            _destination.Write(buffer, offset, count);
        }

        public override bool CanRead => _destination.CanRead;

        public override bool CanSeek => _destination.CanSeek;

        public override bool CanWrite => _destination.CanWrite;

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
