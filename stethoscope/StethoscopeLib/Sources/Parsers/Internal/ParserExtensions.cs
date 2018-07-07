using System;
using System.Collections.Generic;
using System.Text;
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

            public ByteArraySource(byte[] array)
            {
                data = array ?? throw new ArgumentNullException("concatOn");
            }

            public bool CanSeek => true;
            public long Length => data.Length;
            public long Position { get; set; } //XXX should we restrict position?

            public void Dispose()
            {
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                var dataPos = Position;
                var dataCountRemaining = data.Length - dataPos;
                if (dataCountRemaining >= count)
                {
                    Buffer.BlockCopy(data, (int)dataPos, buffer, offset, count);
                    Position = dataPos + count;
                    return count;
                }
                else
                {
                    var dataCountToCopy = (int)dataCountRemaining;
                    Buffer.BlockCopy(data, (int)dataPos, buffer, offset, dataCountToCopy);
                    Position = data.Length;
                    return dataCountToCopy;
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
