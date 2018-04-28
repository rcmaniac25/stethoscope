using LogTracker.Parsers;

using NUnit.Framework;

namespace LogTracker.Tests
{
    [TestFixture(TestOf = typeof(ParserUtil))]
    public class ParserUtilTests
    {
        [TestCase(ParserPathElementFieldType.Unknown, ExpectedResult = null)]
        [TestCase(ParserPathElementFieldType.NotAValue, ExpectedResult = null)]
        public object InvalidCastType(object castType) // Parameter can't be a ParserPathElementFieldType because it would expose a internal/private type publically. Those types can be used as params though
        {
            return ParserUtil.CastField("true", (ParserPathElementFieldType)castType);
        }

        [TestCase("", ExpectedResult = null)]
        [TestCase("true", ExpectedResult = true)]
        [TestCase("false", ExpectedResult = false)]
        [TestCase("TRUE", ExpectedResult = true)]
        [TestCase("FALSE", ExpectedResult = false)]
        [TestCase("True", ExpectedResult = true)]
        [TestCase("False", ExpectedResult = false)]
        [TestCase("TrUe", ExpectedResult = true)]
        [TestCase("FaLsE", ExpectedResult = false)]
        public object CastBool(string boolCast)
        {
            return ParserUtil.CastField(boolCast, ParserPathElementFieldType.Bool);
        }

        //TODO: CastField

        //TODO: ParsePath (and ParseFieldType)
    }
}
