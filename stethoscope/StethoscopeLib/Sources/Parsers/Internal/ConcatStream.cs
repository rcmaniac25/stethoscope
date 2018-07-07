using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Stethoscope.Parsers.Internal
{
    // Based off https://stackoverflow.com/a/3879208/492347 (https://github.com/lassevk/Streams/blob/master/Streams/CombinedStream.cs) though no code was directly used. It was used as a reference to see how they did something.

    public interface IConcatStreamSource : IDisposable
    {
        bool CanSeek { get; }
        long Length { get; }
        long Position { get; set; }

        int Read(byte[] buffer, int offset, int count);
    }

    public class ConcatStream : Stream
    {
        [Flags]
        private enum DirtyProperties
        {
            Clean = 0,

            Seek = 0x1,
            Length = 0x2,

            All = Seek | Length
        }

        private bool disposed = false;

        private List<IConcatStreamSource> sources = new List<IConcatStreamSource>();
        private long absPos = 0L;
        private long posOffset = 0L;
        private int sourceIndex = 0;

        private DirtyProperties propertyDirty = DirtyProperties.All;
        private bool dCanSeek = false;
        private long dLength = 0L;

        #region Properties

        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanSeek
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(ConcatStream));
                }
                lock (sources)
                {
                    if ((propertyDirty & DirtyProperties.Seek) == DirtyProperties.Seek)
                    {
                        dCanSeek = true;
                        foreach (var source in sources)
                        {
                            if (!source.CanSeek)
                            {
                                dCanSeek = false;
                                break;
                            }
                        }
                        propertyDirty &= ~DirtyProperties.Seek;
                    }
                }
                return dCanSeek;
            }
        }
        public override long Length
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(ConcatStream));
                }
                lock (sources)
                {
                    if ((propertyDirty & DirtyProperties.Length) == DirtyProperties.Length)
                    {
                        dLength = 0L;
                        foreach (var source in sources)
                        {
                            dLength += source.Length;
                        }
                        propertyDirty &= ~DirtyProperties.Length;
                    }
                }
                return dLength;
            }
        }
        public override long Position
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(ConcatStream));
                }
                return absPos;
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        #endregion

        #region AppendSource

        public void AppendSource(IConcatStreamSource source)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ConcatStream));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            lock (sources)
            {
                if (sources.Count == 0)
                {
                    var pos = 0L;
                    try
                    {
                        pos = source.Position;
                    }
                    catch
                    {
                        throw new InvalidOperationException("Cannot get source position of first source");
                    }
                    absPos = pos;
                    posOffset = 0;
                    if (pos != 0)
                    {
                        //TODO: need to take index into account
                    }
                }
                //XXX if sources count is not 0, and the new source would change if CanSeek can be used, then we can dispose of sources we're done with since we can't seek back
                propertyDirty = DirtyProperties.All;
                sources.Add(source);
            }
        }

        #endregion

        public override void Flush()
        {
            throw new NotSupportedException($"Flush is not supported for {nameof(ConcatStream)}");
        }

        #region Read

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ConcatStream));
            }
            //TODO: remember to avoid usage of Position and Length as streaming logs won't support those parameters
            return 0;
        }

        #endregion

        #region Seek

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ConcatStream));
            }
            if (!CanSeek)
            {
                throw new NotSupportedException($"Sources for {nameof(ConcatStream)} do not all support seeking");
            }
            var maxLength = -1L;
            try
            {
                maxLength = Length;
            }
            catch
            {
            }
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0)
                    {
                        throw new ArgumentException("Seek position is before start of stream", nameof(offset));
                    }
                    else if (maxLength >= 0 && offset > maxLength)
                    {
                        throw new ArgumentException("Seek position is after the end of the stream", nameof(offset));
                    }
                    //TODO: remember to avoid usage of Position and Length as streaming logs won't support those parameters
                    break;
                case SeekOrigin.Current:
                    var newPos = absPos + offset;
                    if (newPos < 0)
                    {
                        throw new ArgumentException("Seek position is before start of stream", nameof(offset));
                    }
                    else if (maxLength >= 0 && newPos > maxLength)
                    {
                        throw new ArgumentException("Seek position is after the end of the stream", nameof(offset));
                    }
                    //TODO: remember to avoid usage of Position and Length as streaming logs won't support those parameters
                    break;
                case SeekOrigin.End:
                    if (maxLength == -1)
                    {
                        throw new InvalidOperationException("Could not get the length of the stream to identify the end of it");
                    }
                    newPos = maxLength + offset;
                    if (newPos < 0)
                    {
                        throw new ArgumentException("Seek position is before start of stream", nameof(offset));
                    }
                    else if (newPos > maxLength)
                    {
                        throw new ArgumentException("Seek position is after the end of the stream", nameof(offset));
                    }
                    //TODO
                    break;
            }
            return absPos;
        }

        #endregion

        public override void SetLength(long value)
        {
            throw new NotSupportedException($"SetLength is not supported for {nameof(ConcatStream)}");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException($"Write is not supported for {nameof(ConcatStream)}");
        }

        protected override void Dispose(bool disposing)
        {
            disposed = true;
            foreach (var source in sources)
            {
                try
                {
                    source.Dispose();
                }
                catch
                {
                }
            }
            sources.Clear();
        }
    }
}
