using System;
using System.IO;

namespace Stethoscope.Parsers.Internal
{
    public static class ParserExtensions
    {
        #region Stream Sources

        #region ByteArraySource

        private class ByteArraySource : IConcatStreamSource
        {
            private byte[] data;
            private long pos;

            public ByteArraySource(byte[] array)
            {
                data = array ?? throw new ArgumentNullException("concatOn");
            }

            public bool CanSeek => true;
            public long Length => data.LongLength;
            public long Position
            {
                get
                {
                    return pos;
                }
                set
                {
                    if (value < 0 || value > data.LongLength)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }
                    pos = value;
                }
            }

            public void Dispose()
            {
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                var dataCountRemaining = data.LongLength - pos;
                if (dataCountRemaining <= 0)
                {
                    return 0;
                }
                if (dataCountRemaining >= count)
                {
                    Buffer.BlockCopy(data, (int)pos, buffer, offset, count);
                    pos += count;
                    return count;
                }
                else
                {
                    Buffer.BlockCopy(data, (int)pos, buffer, offset, (int)dataCountRemaining);
                    pos = data.LongLength;
                    return (int)dataCountRemaining;
                }
            }
        }

        #endregion

        #region StreamSource

        private class StreamSource : IConcatStreamSource
        {
            private Stream stream;

            public StreamSource(Stream stream)
            {
                this.stream = stream ?? throw new ArgumentNullException("concatOn");
                if (!stream.CanRead)
                {
                    throw new ArgumentException("Stream must be readable", "concatOn");
                }
            }

            public bool CanSeek => stream.CanSeek;
            public long Length => stream.Length;
            public long Position { get => stream.Position; set => stream.Position = value; }

            public void Dispose()
            {
                stream.Dispose();
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                return stream.Read(buffer, offset, count);
            }
        }

        #endregion

        #endregion

        public static Stream Append(this byte[] array, Stream concatOn)
        {
            return new ConcatStream().Append(array).Append(concatOn);
        }

        public static Stream Append(this Stream stream, Stream concatOn)
        {
            return stream.Append(new StreamSource(concatOn));
        }

        public static Stream Append(this Stream stream, byte[] concatOn)
        {
            return stream.Append(new ByteArraySource(concatOn));
        }

        public static Stream Append(this Stream stream, IConcatStreamSource concatOn)
        {
            if (stream is ConcatStream concatStream)
            {
                concatStream.AppendSource(concatOn);
                return stream;
            }
            return new ConcatStream().Append(stream).Append(concatOn);
        }
    }
}
