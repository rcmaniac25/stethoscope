using LogTracker.Parsers;

using NUnit.Framework;

using System.Collections.Generic;

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
        [TestCase("vanilla", ExpectedResult = null)]
        public object CastBool(string value)
        {
            return ParserUtil.CastField(value, ParserPathElementFieldType.Bool);
        }

        [TestCase("", ExpectedResult = "")]
        [TestCase("  ", ExpectedResult = "  ")]
        [TestCase("hi", ExpectedResult = "hi")]
        public object CastString(string value)
        {
            return ParserUtil.CastField(value, ParserPathElementFieldType.String);
        }

        [TestCase("", ExpectedResult = null)]
        [TestCase("0", ExpectedResult = 0)]
        [TestCase("12345", ExpectedResult = 12345)]
        [TestCase("-4321", ExpectedResult = -4321)]
        [TestCase("chocolate", ExpectedResult = null)]
        public object CastInt(string value)
        {
            return ParserUtil.CastField(value, ParserPathElementFieldType.Int);
        }

        //TODO: CastField: KeyValue (will need to do something similar to LogEntryTests.EqualsObject)

        //TODO: ParsePath (and ParseFieldType)
    }
}
