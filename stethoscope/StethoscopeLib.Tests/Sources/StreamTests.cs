﻿using NSubstitute;

using NUnit.Framework;

using Stethoscope.Parsers.Internal;

namespace Stethoscope.Tests
{
    [TestFixture]
    public class StreamTests
    {
        private const int StreamSourceDefaultLength = 20;

        private IConcatStreamSource concatStreamSourceData;
        private IConcatStreamSource concatStreamSourceDataUsed;
        private IConcatStreamSource concatStreamSourceNoData;

        [SetUp]
        public void Setup()
        {
            concatStreamSourceData = Substitute.For<IConcatStreamSource>();
            concatStreamSourceDataUsed = Substitute.For<IConcatStreamSource>();
            concatStreamSourceNoData = Substitute.For<IConcatStreamSource>();

            concatStreamSourceData.Position.Returns(0);
            concatStreamSourceData.Length.Returns(StreamSourceDefaultLength);
            concatStreamSourceData.CanSeek.Returns(true);

            concatStreamSourceDataUsed.Position.Returns(StreamSourceDefaultLength);
            concatStreamSourceDataUsed.Length.Returns(StreamSourceDefaultLength);
            concatStreamSourceDataUsed.CanSeek.Returns(true);

            concatStreamSourceNoData.Position.Returns(0);
            concatStreamSourceNoData.Length.Returns(0);
            concatStreamSourceNoData.CanSeek.Returns(true);
        }

        #region Empty Stream

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamCanRead()
        {
            var cs = new ConcatStream();

            Assert.That(cs.CanRead, Is.True);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamCanSeek()
        {
            var cs = new ConcatStream();

            Assert.That(cs.CanSeek, Is.True);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamCanTimeout()
        {
            var cs = new ConcatStream();

            Assert.That(cs.CanTimeout, Is.False);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamCanWrite()
        {
            var cs = new ConcatStream();

            Assert.That(cs.CanWrite, Is.False);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamPosition()
        {
            var cs = new ConcatStream();

            Assert.That(cs.Position, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamLength()
        {
            var cs = new ConcatStream();

            Assert.That(cs.Length, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamReadZero()
        {
            var cs = new ConcatStream();

            var buffer = new byte[0];
            var res = cs.Read(buffer, 0, 0);
            Assert.That(res, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamReadOne()
        {
            var cs = new ConcatStream();

            var buffer = new byte[1];
            Assert.That(buffer[0], Is.Zero);

            var res = cs.Read(buffer, 0, 1);
            Assert.That(res, Is.Zero);

            Assert.That(buffer[0], Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamReadByte()
        {
            var cs = new ConcatStream();
            
            Assert.That(cs.ReadByte(), Is.EqualTo(-1));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamSeekBeginZero()
        {
            var cs = new ConcatStream();

            var res = cs.Seek(0, System.IO.SeekOrigin.Begin);
            Assert.That(res, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamSeekBegin()
        {
            var cs = new ConcatStream();

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                cs.Seek(1, System.IO.SeekOrigin.Begin);
            });
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamSeekCurrentZero()
        {
            var cs = new ConcatStream();

            var res = cs.Seek(0, System.IO.SeekOrigin.Current);
            Assert.That(res, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamSeekCurrent()
        {
            var cs = new ConcatStream();

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                cs.Seek(1, System.IO.SeekOrigin.Current);
            });
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamSeekEndZero()
        {
            var cs = new ConcatStream();

            var res = cs.Seek(0, System.IO.SeekOrigin.End);
            Assert.That(res, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void EmptyStreamSeekEnd()
        {
            var cs = new ConcatStream();

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                cs.Seek(1, System.IO.SeekOrigin.End);
            });
        }

        #endregion

        #region Single Stream

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamCanRead()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.CanRead, Is.True);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamCanSeek()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.CanSeek, Is.True);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamCanSeekFalse()
        {
            concatStreamSourceData.CanSeek.Returns(false);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.CanSeek, Is.False);
        }
        
        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamCanTimeout()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.CanTimeout, Is.False);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamCanWrite()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.CanWrite, Is.False);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamPosition()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.Position, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamPositionOffset()
        {
            concatStreamSourceData.Position.Returns(10); // Stream source should continue operating without the ConcatStream caring, unless it's the first source

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.Position, Is.EqualTo(10));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamLength()
        {
            concatStreamSourceData.Length.Returns(0);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.Length, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamLengthAlt()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.Length, Is.EqualTo(StreamSourceDefaultLength));
        }
        
        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamReadZero()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var buffer = new byte[0];
            var res = cs.Read(buffer, 0, 0);
            Assert.That(res, Is.Zero);

            concatStreamSourceData.DidNotReceiveWithAnyArgs().Read(null, 0, 0);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamReadOne()
        {
            concatStreamSourceData.Read(Arg.Is<byte[]>(x => x != null), 0, 1).Returns(1).AndDoes(i => i.ArgAt<byte[]>(0)[0] = 10);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var buffer = new byte[1];
            Assert.That(buffer[0], Is.Zero);

            var res = cs.Read(buffer, 0, 1);
            Assert.That(res, Is.EqualTo(1));

            Assert.That(buffer[0], Is.EqualTo(10));

            concatStreamSourceData.Received().Read(Arg.Any<byte[]>(), 0, 1);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamReadOnePositionIsLength()
        {
            concatStreamSourceData.Position.Returns(1);
            concatStreamSourceData.Length.Returns(1);
            concatStreamSourceData.Read(null, 0, 0).ReturnsForAnyArgs(0);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var buffer = new byte[1];
            Assert.That(buffer[0], Is.Zero);

            var res = cs.Read(buffer, 0, 1);
            Assert.That(res, Is.Zero);

            Assert.That(buffer[0], Is.Zero);

            concatStreamSourceData.Received().Read(Arg.Any<byte[]>(), 0, 1);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamReadByte()
        {
            concatStreamSourceData.Read(Arg.Is<byte[]>(x => x != null), 0, 1).Returns(1).AndDoes(i => i.ArgAt<byte[]>(0)[0] = 10);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.ReadByte(), Is.EqualTo(10));
            
            concatStreamSourceData.Received().Read(Arg.Any<byte[]>(), 0, 1);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamReadBytePositionIsLength()
        {
            concatStreamSourceData.Position.Returns(1);
            concatStreamSourceData.Length.Returns(1);
            concatStreamSourceData.Read(null, 0, 0).ReturnsForAnyArgs(0);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.ReadByte(), Is.EqualTo(-1));

            concatStreamSourceData.Received().Read(Arg.Any<byte[]>(), 0, 1);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekBeginZero()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(0, System.IO.SeekOrigin.Begin);
            Assert.That(res, Is.Zero);

            concatStreamSourceData.DidNotReceiveWithAnyArgs().Position = 0;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekBeginZeroChanged()
        {
            concatStreamSourceData.Position.Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(0, System.IO.SeekOrigin.Begin);
            Assert.That(res, Is.Zero);

            concatStreamSourceData.ReceivedWithAnyArgs().Position = 0;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekBeginChanged()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(1, System.IO.SeekOrigin.Begin);
            Assert.That(res, Is.EqualTo(1));

            concatStreamSourceData.ReceivedWithAnyArgs().Position = 1;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekBegin()
        {
            concatStreamSourceData.Position.Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(1, System.IO.SeekOrigin.Begin);
            Assert.That(res, Is.EqualTo(1));

            concatStreamSourceData.DidNotReceiveWithAnyArgs().Position = 0;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekCurrentZero()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(0, System.IO.SeekOrigin.Current);
            Assert.That(res, Is.Zero);

            concatStreamSourceData.DidNotReceiveWithAnyArgs().Position = 0;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekCurrentZeroChanged()
        {
            concatStreamSourceData.Position.Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(0, System.IO.SeekOrigin.Current);
            Assert.That(res, Is.EqualTo(1));

            concatStreamSourceData.DidNotReceiveWithAnyArgs().Position = 0;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekCurrentChanged()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(1, System.IO.SeekOrigin.Current);
            Assert.That(res, Is.EqualTo(1));

            concatStreamSourceData.ReceivedWithAnyArgs().Position = 1;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekCurrent()
        {
            concatStreamSourceData.Position.Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(1, System.IO.SeekOrigin.Current);
            Assert.That(res, Is.EqualTo(2));

            concatStreamSourceData.ReceivedWithAnyArgs().Position = 2;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekCurrentInverse()
        {
            concatStreamSourceData.Position.Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(-1, System.IO.SeekOrigin.Current);
            Assert.That(res, Is.Zero);

            concatStreamSourceData.ReceivedWithAnyArgs().Position = 0;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekEndZero()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(0, System.IO.SeekOrigin.End);
            Assert.That(res, Is.EqualTo(StreamSourceDefaultLength));

            concatStreamSourceData.ReceivedWithAnyArgs().Position = StreamSourceDefaultLength;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekEndZeroChanged()
        {
            concatStreamSourceData.Position.Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(0, System.IO.SeekOrigin.End);
            Assert.That(res, Is.EqualTo(StreamSourceDefaultLength));

            concatStreamSourceData.ReceivedWithAnyArgs().Position = StreamSourceDefaultLength;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekEndChanged()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                cs.Seek(1, System.IO.SeekOrigin.End);
            });
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekEnd()
        {
            concatStreamSourceData.Position.Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                cs.Seek(1, System.IO.SeekOrigin.End);
            });
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekEndChangedInverse()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(-1, System.IO.SeekOrigin.End);
            Assert.That(res, Is.EqualTo(StreamSourceDefaultLength - 1));

            concatStreamSourceData.ReceivedWithAnyArgs().Position = StreamSourceDefaultLength - 1;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekEndInverse()
        {
            concatStreamSourceData.Position.Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(-1, System.IO.SeekOrigin.End);
            Assert.That(res, Is.EqualTo(StreamSourceDefaultLength - 1));

            concatStreamSourceData.ReceivedWithAnyArgs().Position = StreamSourceDefaultLength - 1;
        }

        #endregion

        #region Two Streams

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamCanSeek()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.CanSeek, Is.True);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamOneNotSeekableCanSeek()
        {
            concatStreamSourceData.CanSeek.Returns(false);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.CanSeek, Is.False);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamOneNotSeekableCanSeekReversed()
        {
            concatStreamSourceDataUsed.CanSeek.Returns(false);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.CanSeek, Is.False);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamBothNotSeekableCanSeek()
        {
            concatStreamSourceDataUsed.CanSeek.Returns(false);
            concatStreamSourceData.CanSeek.Returns(false);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.CanSeek, Is.False);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamLength()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.CanSeek, Is.True);
            Assert.That(cs.Length, Is.EqualTo(StreamSourceDefaultLength * 2));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamOneNotSeekableLength()
        {
            concatStreamSourceDataUsed.CanSeek.Returns(false);
            concatStreamSourceDataUsed.Length.Returns(c => throw new System.NotSupportedException()); // Streams like FileStream will throw NotSupportedException if not seekable, but Length is called.

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.CanSeek, Is.False);
            Assert.Throws<System.NotSupportedException>(() =>
            {
                var len = cs.Length;
            });
        }

        private static int GetNumberOfSources(ConcatStream stream)
        {
            var field = typeof(ConcatStream).GetField("sources", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sources = field.GetValue(stream) as System.Collections.Generic.List<IConcatStreamSource>;
            return sources.Count;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourceSeekablesNoOptimize()
        {
            var cs = new ConcatStream();
            Assert.That(GetNumberOfSources(cs), Is.Zero);

            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData, false);

            Assert.That(GetNumberOfSources(cs), Is.EqualTo(2));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourceSeekablesOptimize()
        {
            var cs = new ConcatStream();
            Assert.That(GetNumberOfSources(cs), Is.Zero);

            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData, true);

            Assert.That(GetNumberOfSources(cs), Is.EqualTo(2));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourceNotSeekablesNoOptimize()
        {
            concatStreamSourceData.CanSeek.Returns(false);

            var cs = new ConcatStream();
            Assert.That(GetNumberOfSources(cs), Is.Zero);

            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData, false);

            Assert.That(GetNumberOfSources(cs), Is.EqualTo(2));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourceNotSeekablesOptimize()
        {
            concatStreamSourceData.CanSeek.Returns(false);

            var cs = new ConcatStream();
            Assert.That(GetNumberOfSources(cs), Is.Zero);

            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData, true);

            Assert.That(GetNumberOfSources(cs), Is.EqualTo(1));
            concatStreamSourceDataUsed.Received().Dispose();
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourcePosition()
        {
            var cs = new ConcatStream();
            Assert.That(cs.Position, Is.Zero);

            cs.AppendSource(concatStreamSourceDataUsed);

            Assert.That(cs.Position, Is.EqualTo(StreamSourceDefaultLength));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourcePositionDouble()
        {
            var cs = new ConcatStream();
            Assert.That(cs.Position, Is.Zero);

            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceDataUsed);

            Assert.That(cs.Position, Is.EqualTo(StreamSourceDefaultLength * 2));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourcePositionPlusZero()
        {
            var cs = new ConcatStream();
            Assert.That(cs.Position, Is.Zero);

            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.Position, Is.EqualTo(StreamSourceDefaultLength));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourcePositionPlusTen()
        {
            concatStreamSourceData.Position.Returns(10);

            var cs = new ConcatStream();
            Assert.That(cs.Position, Is.Zero);

            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            Assert.That(cs.Position, Is.EqualTo(StreamSourceDefaultLength + 10));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourcePositionPlusTenPlusInvalidTen()
        {
            concatStreamSourceData.Position.Returns(10);

            var cs = new ConcatStream();
            Assert.That(cs.Position, Is.Zero);

            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);
            Assert.Throws<System.ArgumentException>(() =>
            {
                cs.AppendSource(concatStreamSourceData);
            });

            Assert.That(cs.Position, Is.EqualTo(StreamSourceDefaultLength + 10));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourcePositionPlusTenPlusNonSeekable()
        {
            concatStreamSourceData.Position.Returns(10);

            var cs = new ConcatStream();
            Assert.That(cs.Position, Is.Zero);
            Assert.That(cs.CanSeek, Is.True);

            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);
            Assert.That(cs.CanSeek, Is.True);

            concatStreamSourceData.CanSeek.Returns(false);

            cs.AppendSource(concatStreamSourceData);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourcePostSeek()
        {
            concatStreamSourceData.Position.Returns(10);

            var cs = new ConcatStream();
            Assert.That(cs.Position, Is.Zero);

            cs.AppendSource(concatStreamSourceDataUsed);

            Assert.That(cs.Position, Is.EqualTo(StreamSourceDefaultLength));

            cs.Seek(10, System.IO.SeekOrigin.Begin);
            concatStreamSourceDataUsed.Received().Position = 10;
            Assert.That(cs.Position, Is.EqualTo(10));
            concatStreamSourceDataUsed.Position.Returns(10); // We first check we got the position, then we ensure we actually return the value

            cs.Seek(0, System.IO.SeekOrigin.End);
            concatStreamSourceDataUsed.Received().Position = StreamSourceDefaultLength;
            Assert.That(cs.Position, Is.EqualTo(StreamSourceDefaultLength));
            concatStreamSourceDataUsed.Position.Returns(StreamSourceDefaultLength);

            Assert.Throws<System.InvalidOperationException>(() =>
            {
                cs.AppendSource(concatStreamSourceData);
            });
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamAppendSourcePostRead()
        {
            concatStreamSourceData.Position.Returns(10);

            var cs = new ConcatStream();
            Assert.That(cs.Position, Is.Zero);

            cs.AppendSource(concatStreamSourceDataUsed);

            Assert.That(cs.Position, Is.EqualTo(StreamSourceDefaultLength));

            cs.Seek(StreamSourceDefaultLength - 1, System.IO.SeekOrigin.Begin);
            concatStreamSourceDataUsed.Received().Position = StreamSourceDefaultLength - 1;
            Assert.That(cs.Position, Is.EqualTo(StreamSourceDefaultLength - 1));
            concatStreamSourceDataUsed.Position.Returns(StreamSourceDefaultLength - 1);

            concatStreamSourceDataUsed.Read(Arg.Any<byte[]>(), 0, 1).Returns(1);
            cs.ReadByte();
            concatStreamSourceDataUsed.Position.Returns(StreamSourceDefaultLength);

            Assert.Throws<System.InvalidOperationException>(() =>
            {
                cs.AppendSource(concatStreamSourceData);
            });
        }

        //TODO: read 1 byte (first stream, second stream, end of first stream, beginning of second stream, read 1 byte twice [1 from end of first, 1 from start of second])

        //TODO: read 2 byte (first stream into second stream)

        //TODO: seek (a whole bunch, including non-seekable concat)

        #endregion

        #region Byte Array Stream

        //TODO: byte array tests

        #endregion

        #region System.IO Stream

        //TODO: stream tests

        #endregion
    }
}