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

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsReadOneFirstStream()
        {
            concatStreamSourceData.Position.Returns(10);
            concatStreamSourceData.Read(Arg.Any<byte[]>(), 0, 1).Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);
            cs.AppendSource(concatStreamSourceNoData);

            var value = cs.ReadByte();
            Assert.That(value, Is.Not.EqualTo(-1));

            concatStreamSourceData.Received(1).Read(Arg.Any<byte[]>(), 0, 1);
            concatStreamSourceNoData.DidNotReceiveWithAnyArgs().Read(null, 0, 0);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsReadOneSecondStream()
        {
            concatStreamSourceData.Position.Returns(10);
            concatStreamSourceData.Read(Arg.Any<byte[]>(), 0, 1).Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            var value = cs.ReadByte();
            Assert.That(value, Is.Not.EqualTo(-1));

            concatStreamSourceData.Received(1).Read(Arg.Any<byte[]>(), 0, 1);
            concatStreamSourceDataUsed.DidNotReceiveWithAnyArgs().Read(null, 0, 0);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsReadOneAtEndFirstStream()
        {
            concatStreamSourceData.Position.Returns(StreamSourceDefaultLength - 1);
            concatStreamSourceData.Read(Arg.Any<byte[]>(), 0, 1).Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);
            cs.AppendSource(concatStreamSourceNoData);

            var value = cs.ReadByte();
            Assert.That(value, Is.Not.EqualTo(-1));

            concatStreamSourceData.Received(1).Read(Arg.Any<byte[]>(), 0, 1);
            concatStreamSourceNoData.DidNotReceiveWithAnyArgs().Read(null, 0, 0);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsReadOneAtStartSecondStream()
        {
            concatStreamSourceData.Read(Arg.Any<byte[]>(), 0, 1).Returns(1);
            concatStreamSourceDataUsed.Read(null, 0, 0).ReturnsForAnyArgs(0);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            var value = cs.ReadByte();
            Assert.That(value, Is.Not.EqualTo(-1));

            concatStreamSourceData.Received(1).Read(Arg.Any<byte[]>(), 0, 1);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsReadTwoIndependent()
        {
            concatStreamSourceData.Position.Returns(StreamSourceDefaultLength - 1);
            concatStreamSourceData.Read(Arg.Any<byte[]>(), 0, 1).Returns(1, 0);

            concatStreamSourceNoData.Length.Returns(StreamSourceDefaultLength); // Just to prevent making and setting up a new mock
            concatStreamSourceNoData.Read(Arg.Any<byte[]>(), 0, 1).Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);
            cs.AppendSource(concatStreamSourceNoData);

            var value = cs.ReadByte();
            Assert.That(value, Is.Not.EqualTo(-1));
            value = cs.ReadByte();
            Assert.That(value, Is.Not.EqualTo(-1));

            concatStreamSourceData.Received(2).Read(Arg.Any<byte[]>(), 0, 1);
            concatStreamSourceNoData.Received(1).Read(Arg.Any<byte[]>(), 0, 1);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsReadTwo()
        {
            concatStreamSourceData.Position.Returns(StreamSourceDefaultLength - 1);
            concatStreamSourceData.Read(Arg.Any<byte[]>(), 0, 2).Returns(1);
            concatStreamSourceData.Read(Arg.Any<byte[]>(), 1, 1).Returns(0);

            concatStreamSourceNoData.Length.Returns(StreamSourceDefaultLength); // Just to prevent making and setting up a new mock
            concatStreamSourceNoData.Read(Arg.Any<byte[]>(), 1, 1).Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);
            cs.AppendSource(concatStreamSourceNoData);

            var buffer = new byte[2];
            var count = cs.Read(buffer, 0, 2);
            Assert.That(count, Is.EqualTo(2));

            concatStreamSourceData.ReceivedWithAnyArgs(2).Read(null, 0, 0);
            concatStreamSourceNoData.Received(1).Read(Arg.Any<byte[]>(), 1, 1);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsSeekFirstCurrent()
        {
            concatStreamSourceData.Position.Returns(1);

            concatStreamSourceNoData.Length.Returns(StreamSourceDefaultLength); // Just to prevent making and setting up a new mock

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);
            cs.AppendSource(concatStreamSourceNoData);

            var res = cs.Seek(1, System.IO.SeekOrigin.Current);
            Assert.That(res, Is.EqualTo(2));

            concatStreamSourceData.ReceivedWithAnyArgs().Position = 2;
            concatStreamSourceNoData.DidNotReceiveWithAnyArgs().Position = 0;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsSeekFirstBegin()
        {
            concatStreamSourceNoData.Length.Returns(StreamSourceDefaultLength); // Just to prevent making and setting up a new mock

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);
            cs.AppendSource(concatStreamSourceNoData);

            var res = cs.Seek(1, System.IO.SeekOrigin.Begin);
            Assert.That(res, Is.EqualTo(1));

            concatStreamSourceData.ReceivedWithAnyArgs().Position = 1;
            concatStreamSourceNoData.DidNotReceiveWithAnyArgs().Position = 0;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsSeekIntoSecond()
        {
            concatStreamSourceData.Position.Returns(StreamSourceDefaultLength - 1);

            concatStreamSourceNoData.Length.Returns(StreamSourceDefaultLength); // Just to prevent making and setting up a new mock

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceData);
            cs.AppendSource(concatStreamSourceNoData);

            var res = cs.Seek(2, System.IO.SeekOrigin.Current);
            Assert.That(res, Is.EqualTo(StreamSourceDefaultLength + 1));

            concatStreamSourceData.ReceivedWithAnyArgs().Position = StreamSourceDefaultLength;
            concatStreamSourceNoData.ReceivedWithAnyArgs().Position = 1;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsSeekIntoFirst()
        {
            concatStreamSourceData.Position.Returns(1);
            
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(-2, System.IO.SeekOrigin.Current);
            Assert.That(res, Is.EqualTo(StreamSourceDefaultLength - 1));

            concatStreamSourceDataUsed.ReceivedWithAnyArgs().Position = StreamSourceDefaultLength - 1;
            concatStreamSourceData.ReceivedWithAnyArgs().Position = 0;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsSeekSecondEnd()
        {
            concatStreamSourceData.Position.Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            var res = cs.Seek(-1, System.IO.SeekOrigin.End);
            Assert.That(res, Is.EqualTo((StreamSourceDefaultLength * 2) - 1));

            concatStreamSourceDataUsed.DidNotReceiveWithAnyArgs().Position = 0;
            concatStreamSourceData.ReceivedWithAnyArgs().Position = StreamSourceDefaultLength - 1;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void TwoStreamsSeekNonSeekable()
        {
            concatStreamSourceData.CanSeek.Returns(false);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSourceDataUsed);
            cs.AppendSource(concatStreamSourceData);

            Assert.Throws<System.NotSupportedException>(() =>
            {
                cs.Seek(-1, System.IO.SeekOrigin.End);
            });
        }

        #endregion

        #region Byte Array Stream

        [Test(TestOf = typeof(ConcatStream))]
        public void ByteArrayNull()
        {
            byte[] data = null;

            var cs = new ConcatStream();

            Assert.Throws<System.ArgumentNullException>(() =>
            {
                cs.Append(data);
            });
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void ByteArrayAppendEquality()
        {
            var data = new byte[0];

            var cs = new ConcatStream();
            var stream = cs.Append(data);

            Assert.That(cs, Is.EqualTo(stream));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void ByteArrayEmptyLength()
        {
            var data = new byte[0];

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.That(cs.Length, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void ByteArrayLength()
        {
            var data = new byte[10];

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.That(cs.Length, Is.EqualTo(10));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void ByteArrayCanSeek()
        {
            var data = new byte[0];

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.That(cs.CanSeek, Is.True);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void ByteArrayGetPosition()
        {
            var data = new byte[10];

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.That(cs.Position, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void ByteArraySetPosition()
        {
            var data = new byte[10];

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.That(cs.Position, Is.Zero);
            cs.Position = 5;
            Assert.That(cs.Position, Is.EqualTo(5));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void ByteArraySetPositionInvalid()
        {
            var data = new byte[10];

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                cs.Position = -1;
            });
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                cs.Position = 11;
            });
        }

        #region ConcatStream Read Test Functions

        // Realized later, refactoring this to seperate functions was unnecessary. But it doesn't hurt and is more time to put back together (or awkardly put back together)

        public enum ReadResult
        {
            NullException,
            ArgumentException,
            OutOfRangeException,

            NonZeroResult,
            ZeroResult
        }

        public enum BufferType
        {
            Null,
            Instance
        }

        private const int ReadingSourceEmpty = 0;
        private const int ReadingSourceNotEmpty = 1;
        private static readonly byte[][] ReadingSourceData = new byte[][]
        {
            new byte[0],
            new byte[] { 10, 20, 30 } // Don't go more then 5 elements
        };

        private void ConcatStreamReadTestVerification(BufferType bufferType, int offset, int length, ReadResult expectedResult)
        {
            // Ensure we don't ever make a combo of arguments that will always fail

            if (bufferType == BufferType.Null && expectedResult == ReadResult.NonZeroResult)
            {
                Assert.Fail("Won't be able to check results if buffer is null");
            }
        }

        public void ConcatStreamReadTest(ConcatStream cs, BufferType bufferType, int offset, int length, ReadResult expectedResult)
        {
            ConcatStreamReadTestVerification(bufferType, offset, length, expectedResult);

            byte[] buffer = bufferType == BufferType.Null ? null : new byte[5];

            switch (expectedResult)
            {
                case ReadResult.NullException:
                case ReadResult.ArgumentException:
                case ReadResult.OutOfRangeException:
                    TestDelegate testDelegate = () =>
                    {
                        cs.Read(buffer, offset, length);
                    };
                    switch (expectedResult)
                    {
                        case ReadResult.NullException: Assert.Throws<System.ArgumentNullException>(testDelegate); break;
                        case ReadResult.ArgumentException: Assert.Throws<System.ArgumentException>(testDelegate); break;
                        case ReadResult.OutOfRangeException: Assert.Throws<System.ArgumentOutOfRangeException>(testDelegate); break;
                    }

                    break;

                case ReadResult.ZeroResult:
                    var result = cs.Read(buffer, offset, length);
                    Assert.That(result, Is.Zero);
                    if (bufferType == BufferType.Instance)
                    {
                        Assert.That(buffer, Is.All.Zero);
                    }
                    break;

                case ReadResult.NonZeroResult:
                    result = cs.Read(buffer, offset, length);
                    Assert.That(result, Is.Not.Zero);
                    if (bufferType == BufferType.Instance)
                    {
                        for (int i = offset; i < (result + offset); i++)
                        {
                            Assert.That(buffer[i], Is.EqualTo(ReadingSourceData[ReadingSourceNotEmpty][i - offset]));
                        }
                    }
                    break;

                default:
                    Assert.Fail($"Unknown {nameof(expectedResult)}: {expectedResult}");
                    break;
            }
        }

        #endregion
        
        [TestCase(ReadingSourceEmpty, BufferType.Null, 0, 0, ReadResult.NullException, TestOf = typeof(ConcatStream))]
        [TestCase(ReadingSourceEmpty, BufferType.Instance, 0, 0, ReadResult.ZeroResult, TestOf = typeof(ConcatStream))]
        [TestCase(ReadingSourceEmpty, BufferType.Instance, 0, 1, ReadResult.ZeroResult, TestOf = typeof(ConcatStream))]
        [TestCase(ReadingSourceEmpty, BufferType.Instance, 1, 1, ReadResult.ZeroResult, TestOf = typeof(ConcatStream))]
        [TestCase(ReadingSourceEmpty, BufferType.Instance, -1, 1, ReadResult.OutOfRangeException, TestOf = typeof(ConcatStream))]
        [TestCase(ReadingSourceEmpty, BufferType.Instance, 0, -1, ReadResult.OutOfRangeException, TestOf = typeof(ConcatStream))]
        [TestCase(ReadingSourceNotEmpty, BufferType.Null, 0, 0, ReadResult.NullException, TestOf = typeof(ConcatStream))]
        [TestCase(ReadingSourceNotEmpty, BufferType.Instance, 0, 0, ReadResult.ZeroResult, TestOf = typeof(ConcatStream))]
        [TestCase(ReadingSourceNotEmpty, BufferType.Instance, 0, 1, ReadResult.NonZeroResult, TestOf = typeof(ConcatStream))]
        [TestCase(ReadingSourceNotEmpty, BufferType.Instance, 1, 1, ReadResult.NonZeroResult, TestOf = typeof(ConcatStream))]
        [TestCase(ReadingSourceNotEmpty, BufferType.Instance, -1, 1, ReadResult.OutOfRangeException, TestOf = typeof(ConcatStream))]
        [TestCase(ReadingSourceNotEmpty, BufferType.Instance, 0, -1, ReadResult.OutOfRangeException, TestOf = typeof(ConcatStream))]
        public void ByteArrayRead(int dataIndex, BufferType bufferType, int offset, int length, ReadResult expectedResult)
        {
            var cs = new ConcatStream();
            cs.Append(ReadingSourceData[dataIndex]);

            ConcatStreamReadTest(cs, bufferType, offset, length, expectedResult);
        }

        #endregion

        #region System.IO Stream

        [Test(TestOf = typeof(ConcatStream))]
        public void SystemIOStreamNull()
        {
            System.IO.Stream data = null;

            var cs = new ConcatStream();

            Assert.Throws<System.ArgumentNullException>(() =>
            {
                cs.Append(data);
            });
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void SystemIOAppendEquality()
        {
            var data = new System.IO.MemoryStream();

            var cs = new ConcatStream();
            var stream = cs.Append(data);

            Assert.That(cs, Is.EqualTo(stream));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void SystemIOStreamReadable()
        {
            var data = Substitute.For<System.IO.Stream>();
            data.CanSeek.Returns(true);
            data.CanRead.Returns(true);
            data.Position.Returns(0);
            data.Length.Returns(StreamSourceDefaultLength);

            var cs = new ConcatStream();
            cs.Append(data);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void SystemIOStreamNotReadable()
        {
            var data = Substitute.For<System.IO.Stream>();
            data.CanSeek.Returns(true);
            data.CanRead.Returns(false);
            data.Position.Returns(0);
            data.Length.Returns(StreamSourceDefaultLength);

            var cs = new ConcatStream();
            Assert.Throws<System.ArgumentException>(() =>
            {
                cs.Append(data);
            });
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void SystemIOCanSeek()
        {
            var data = Substitute.For<System.IO.Stream>();
            data.CanSeek.Returns(true);
            data.CanRead.Returns(true);
            data.Position.Returns(0);
            data.Length.Returns(StreamSourceDefaultLength);

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.That(cs.CanSeek, Is.True);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void SystemIOCanSeekFalse()
        {
            var data = Substitute.For<System.IO.Stream>();
            data.CanSeek.Returns(false);
            data.CanRead.Returns(true);
            data.Position.Returns(0);
            data.Length.Returns(StreamSourceDefaultLength);

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.That(cs.CanSeek, Is.False);
        }
        
        [Test(TestOf = typeof(ConcatStream))]
        public void SystemIOGetPosition()
        {
            var data = new System.IO.MemoryStream(new byte[10]);

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.That(cs.Position, Is.Zero);
            Assert.That(data.Position, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void SystemIOSetPosition()
        {
            var data = new System.IO.MemoryStream(new byte[10]);

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.That(cs.Position, Is.Zero);
            Assert.That(data.Position, Is.Zero);
            cs.Position = 5;
            Assert.That(cs.Position, Is.EqualTo(5));
            Assert.That(data.Position, Is.EqualTo(5));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void SystemIOSetPositionInvalid()
        {
            var data = new System.IO.MemoryStream(new byte[10]);

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                cs.Position = -1;
            });
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                cs.Position = 11;
            });
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void SystemIOReadPassthrough()
        {
            // As calls are simply passed through to the stream, it's Stream dependent. So just make sure the stream was effected

            var data = new System.IO.MemoryStream(new byte[10]);

            var cs = new ConcatStream();
            cs.Append(data);

            Assert.That(cs.Position, Is.Zero);
            Assert.That(data.Position, Is.Zero);

            var buffer = new byte[5];
            var read = cs.Read(buffer, 0, 3);

            Assert.That(read, Is.EqualTo(3));
            Assert.That(cs.Position, Is.EqualTo(3));
            Assert.That(data.Position, Is.EqualTo(3));
        }

        #endregion
    }
}
