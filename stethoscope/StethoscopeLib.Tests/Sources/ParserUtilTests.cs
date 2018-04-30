using LogTracker.Parsers;
using LogTracker.Tests.Helpers;

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

        [TestCase(null, ExpectedResult = null)]
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

        [TestCase(null, ExpectedResult = null)]
        [TestCase("", ExpectedResult = "")]
        [TestCase("  ", ExpectedResult = "  ")]
        [TestCase("hi", ExpectedResult = "hi")]
        public object CastString(string value)
        {
            return ParserUtil.CastField(value, ParserPathElementFieldType.String);
        }

        [TestCase(null, ExpectedResult = null)]
        [TestCase("", ExpectedResult = null)]
        [TestCase("0", ExpectedResult = 0)]
        [TestCase("12345", ExpectedResult = 12345)]
        [TestCase("-4321", ExpectedResult = -4321)]
        [TestCase("chocolate", ExpectedResult = null)]
        public object CastInt(string value)
        {
            return ParserUtil.CastField(value, ParserPathElementFieldType.Int);
        }

        private static TestCaseData[] CastKeyValueCases =
        {
            DictionaryTestDataBuilder.TestAgainst(null).For("CastKeyValue(null)").Which().Returns(null),
            DictionaryTestDataBuilder.TestAgainst("").For("CastKeyValue(\"\")").Which().Returns(null),
            DictionaryTestDataBuilder.TestAgainst("string has no key-values").For("CastKeyValue(string has no key-values)").Which().Returns(null),
            DictionaryTestDataBuilder.TestAgainst("string has; no key-values").For("CastKeyValue(string has; no key-values)").Which().Returns(null),
            DictionaryTestDataBuilder.TestAgainst("string has a key-values right=now").For("CastKeyValue(string has a key-values right=now)").WhichWill().ReturnWith("string has a key-values right", "now"),
            DictionaryTestDataBuilder.TestAgainst("string has multiple key=values;right=now").For("CastKeyValue(string has multiple key=values;right=now)").WhichWill().ReturnWith("string has multiple key", "values").And("right", "now"),
            DictionaryTestDataBuilder.TestAgainst("k1=v1;k2=v2").For("CastKeyValue(k1=v1;k2=v2)").WhichWill().ReturnWith("k1", "v1").And("k2", "v2"),
            DictionaryTestDataBuilder.TestAgainst("k1=v1,k2=v2").For("CastKeyValue(k1=v1,k2=v2)").WhichWill().ReturnWith("k1", "v1").And("k2", "v2"),
            DictionaryTestDataBuilder.TestAgainst("\"key=value\"=\"test=pain1\"").For("CastKeyValue(\"key=value\"=\"test=pain1\")").WhichWill().ReturnWith("key=value", "test=pain1"),
            DictionaryTestDataBuilder.TestAgainst("\"key=value;oh=boy\"=\"test=pain1\"").For("CastKeyValue(\"key=value;oh=boy\"=\"test=pain1\")").WhichWill().ReturnWith("key=value;oh=boy", "test=pain1")
        };

        [TestCaseSource("CastKeyValueCases")]
        public object CastKeyValue(string value)
        {
            return ParserUtil.CastField(value, ParserPathElementFieldType.KeyValue);
        }

        //TODO: ParsePath (and ParseFieldType)
    }
}
