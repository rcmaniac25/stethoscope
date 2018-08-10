using NUnit.Framework;

using Stethoscope.Parsers.Internal;

namespace Stethoscope.Tests
{
    [TestFixture]
    public class StreamTests
    {
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
        
        //<use mock for data>

        //TODO: One source with data, some basic property and function tests

        //TODO: one source with no data, some basic property and function tests

        //TODO: two sources with data, some basic property and function tests

        //TODO: byte array tests

        //TODO: stream tests
    }
}
