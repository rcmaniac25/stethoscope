using NSubstitute;

using NUnit.Framework;

using Stethoscope.Parsers.Internal;

namespace Stethoscope.Tests
{
    [TestFixture]
    public class StreamTests
    {
        private IConcatStreamSource concatStreamSource1;
        private IConcatStreamSource concatStreamSource2;

        [SetUp]
        public void Setup()
        {
            concatStreamSource1 = Substitute.For<IConcatStreamSource>();
            concatStreamSource2 = Substitute.For<IConcatStreamSource>();

            concatStreamSource1.Position.Returns(0);
            concatStreamSource1.Length.Returns(20);
            concatStreamSource1.CanSeek.Returns(true);
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

            Assert.Throws<System.ArgumentException>(() =>
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

            Assert.Throws<System.ArgumentException>(() =>
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

            Assert.Throws<System.ArgumentException>(() =>
            {
                cs.Seek(1, System.IO.SeekOrigin.End);
            });
        }

        #endregion

        #region Single Populated Stream

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamCanRead()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            Assert.That(cs.CanRead, Is.True);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamCanSeek()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            Assert.That(cs.CanSeek, Is.True);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamCanSeekFalse()
        {
            concatStreamSource1.CanSeek.Returns(false);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            Assert.That(cs.CanSeek, Is.False);
        }
        
        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamCanTimeout()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            Assert.That(cs.CanTimeout, Is.False);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamCanWrite()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            Assert.That(cs.CanWrite, Is.False);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamPosition()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            Assert.That(cs.Position, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamPositionOffset()
        {
            concatStreamSource1.Position.Returns(10); // Stream source should continue operating without the ConcatStream caring, unless it's the first source

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            Assert.That(cs.Position, Is.EqualTo(10));
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamLength()
        {
            concatStreamSource1.Length.Returns(0);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            Assert.That(cs.Length, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamLengthAlt()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            Assert.That(cs.Length, Is.EqualTo(20));
        }
        
        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamReadZero()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            var buffer = new byte[0];
            var res = cs.Read(buffer, 0, 0);
            Assert.That(res, Is.Zero);

            concatStreamSource1.DidNotReceiveWithAnyArgs().Read(null, 0, 0);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamReadOne()
        {
            concatStreamSource1.Read(Arg.Is<byte[]>(x => x != null), 0, 1).Returns(1).AndDoes(i => i.ArgAt<byte[]>(0)[0] = 10);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            var buffer = new byte[1];
            Assert.That(buffer[0], Is.Zero);

            var res = cs.Read(buffer, 0, 1);
            Assert.That(res, Is.EqualTo(1));

            Assert.That(buffer[0], Is.EqualTo(10));

            concatStreamSource1.Received().Read(Arg.Any<byte[]>(), 0, 1);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamReadOnePositionIsLength()
        {
            concatStreamSource1.Position.Returns(1);
            concatStreamSource1.Length.Returns(1);
            concatStreamSource1.Read(null, 0, 0).ReturnsForAnyArgs(0);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            var buffer = new byte[1];
            Assert.That(buffer[0], Is.Zero);

            var res = cs.Read(buffer, 0, 1);
            Assert.That(res, Is.Zero);

            Assert.That(buffer[0], Is.Zero);

            concatStreamSource1.Received().Read(Arg.Any<byte[]>(), 0, 1);
        }

        //

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamReadByte()
        {
            concatStreamSource1.Read(Arg.Is<byte[]>(x => x != null), 0, 1).Returns(1).AndDoes(i => i.ArgAt<byte[]>(0)[0] = 10);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            Assert.That(cs.ReadByte(), Is.EqualTo(10));
            
            concatStreamSource1.Received().Read(Arg.Any<byte[]>(), 0, 1);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamReadBytePositionIsLength()
        {
            concatStreamSource1.Position.Returns(1);
            concatStreamSource1.Length.Returns(1);
            concatStreamSource1.Read(null, 0, 0).ReturnsForAnyArgs(0);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            Assert.That(cs.ReadByte(), Is.EqualTo(-1));

            concatStreamSource1.Received().Read(Arg.Any<byte[]>(), 0, 1);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekBeginZero()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            var res = cs.Seek(0, System.IO.SeekOrigin.Begin);
            Assert.That(res, Is.Zero);

            concatStreamSource1.DidNotReceiveWithAnyArgs().Position = 0;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekBeginZeroChanged()
        {
            concatStreamSource1.Position.Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            var res = cs.Seek(0, System.IO.SeekOrigin.Begin);
            Assert.That(res, Is.Zero);

            concatStreamSource1.ReceivedWithAnyArgs().Position = 0;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekBeginChanged()
        {
            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            var res = cs.Seek(1, System.IO.SeekOrigin.Begin);
            Assert.That(res, Is.EqualTo(1));

            concatStreamSource1.ReceivedWithAnyArgs().Position = 1;
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekBegin()
        {
            concatStreamSource1.Position.Returns(1);

            var cs = new ConcatStream();
            cs.AppendSource(concatStreamSource1);

            var res = cs.Seek(1, System.IO.SeekOrigin.Begin);
            Assert.That(res, Is.EqualTo(1));

            concatStreamSource1.DidNotReceiveWithAnyArgs().Position = 0;
        }

        //TODO: other seek functions

#if false
        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekCurrentZero()
        {
            var cs = new ConcatStream();

            var res = cs.Seek(0, System.IO.SeekOrigin.Current);
            Assert.That(res, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekCurrent()
        {
            var cs = new ConcatStream();

            Assert.Throws<System.ArgumentException>(() =>
            {
                cs.Seek(1, System.IO.SeekOrigin.Current);
            });
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekEndZero()
        {
            var cs = new ConcatStream();

            var res = cs.Seek(0, System.IO.SeekOrigin.End);
            Assert.That(res, Is.Zero);
        }

        [Test(TestOf = typeof(ConcatStream))]
        public void OnePopulatedStreamSeekEnd()
        {
            var cs = new ConcatStream();

            Assert.Throws<System.ArgumentException>(() =>
            {
                cs.Seek(1, System.IO.SeekOrigin.End);
            });
        }
#endif

        #endregion

        //TODO: one source with no data, some basic property and function tests (don't forget to test "optimized" sources [see AppendSource arguments])

        //TODO: two sources with data, some basic property and function tests

        //TODO: byte array tests

        //TODO: stream tests
    }
}
