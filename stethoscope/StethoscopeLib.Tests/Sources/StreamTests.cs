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

        //TODO: (empty) basic properties and functions

        //<use mock for data>

        //TODO: One source with data, some basic property and function tests

        //TODO: one source with no data, some basic property and function tests

        //TODO: two sources with data, some basic property and function tests

        //TODO: byte array tests

        //TODO: stream tests
    }
}
