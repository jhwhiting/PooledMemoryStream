using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace PooledMemoryStreamDemo
{
    public class PooledMemoryStream : Stream
    {
        private readonly IList<byte[]> _byteArrays = new List<byte[]>(24);

        private readonly ArrayPool<byte> _pool;

        private readonly int _segmentSize;

        private long _length = 0;

        private long _position = 0;

        public PooledMemoryStream(ArrayPool<byte> pool, int segmentSize = 65536)
        {
            _pool = pool;

            _segmentSize = segmentSize;
        }

        public PooledMemoryStream(byte[] bytes, ArrayPool<byte> pool, int segmentSize = 65536) : this(pool, segmentSize)
        {
            Write(bytes, 0, bytes.Length);

            Seek(0, SeekOrigin.Begin);
        }

        public PooledMemoryStream(Stream stream, ArrayPool<byte> pool, int segmentSize = 65536) : this(pool, segmentSize)
        {
            stream.CopyTo(this);
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _length;

        public long Capacity => _byteArrays.Count * _segmentSize;

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        private byte[] GetSegment()
        {
            int index = FillSegments(_position);

            return _byteArrays[index];
        }

        private int FillSegments(long position)
        {
            int index = (int)(position / _segmentSize);

            int count = index + 1;

            while (count > _byteArrays.Count)
            {
                _byteArrays.Add(_pool.Rent(_segmentSize));
            }

            return index;
        }

        public byte[] ToArray() // provided for compatibility; try not to use
        {
            Seek(0, SeekOrigin.Begin);

            byte[] buffer = new byte[_length];

            long count = _length;

            int read = 0;

            while (count > 0)
            {
                read = Read(buffer, read, (int)_length);

                count -= read;
            }

            return buffer;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length) return 0;

            int read = 0;

            while (count > 0)
            {
                int segmentOffset = (int)(_position % _segmentSize);

                byte[] segment = GetSegment();

                while (segmentOffset < _segmentSize)
                {
                    if (count <= 0 || offset >= buffer.Length || _position >= _length) return read;

                    buffer[offset++] = segment[segmentOffset++];

                    count--;
                    read++;

                    _position++;
                }
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                _position = 0;
            }
            else if (origin == SeekOrigin.End)
            {
                _position = _length;
            }

            _position += offset;

            if (_position > _length)
            {
                _position = _length;
            }
            else if (_position < 0)
            {
                _position = 0;
            }

            return _position;
        }

        public override void SetLength(long value)
        {
            FillSegments(value);

            if (_position > value)
            {
                _position = value;
            }

            _length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int segmentOffset = (int)(_position % _segmentSize);

                byte[] segment = GetSegment();

                while (count > 0 && offset < buffer.Length && segmentOffset < _segmentSize)
                {
                    segment[segmentOffset++] = buffer[offset++];

                    _position++;

                    if (_position > _length) _length++;

                    count--;
                }
            }
        }

        public override void Flush() { }

        protected override void Dispose(bool disposing)
        {
            if (disposing) base.Dispose();

            GC.SuppressFinalize(this);
        }

        public override void Close()
        {
            while (_byteArrays.Count > 0)
            {
                int i = _byteArrays.Count - 1;

                byte[] buffer = _byteArrays[i];

                _pool.Return(buffer);

                _byteArrays.RemoveAt(i);
            }

            _length = 0;

            _position = 0;

            Dispose(false);
        }
    }
}