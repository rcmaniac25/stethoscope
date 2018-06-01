using LogTracker.Parsers;
using LogTracker.Tests.Helpers;

using NUnit.Framework;

using System;
using System.Text;

namespace LogTracker.Tests
{
    [TestFixture(TestOf = typeof(ParserUtil))]
    public class ParserUtilTests
    {
        private static readonly Tuple<string, ParserPathElementFieldType>[] PathFieldTypes;
        private static readonly Tuple<string, ParserPathElementType>[] PathElementTypes;

        static ParserUtilTests()
        {
            PathFieldTypes = new Tuple<string, ParserPathElementFieldType>[]
            {
                new Tuple<string, ParserPathElementFieldType>("string", ParserPathElementFieldType.String), // Keep this first, as it's a default

                new Tuple<string, ParserPathElementFieldType>("bool", ParserPathElementFieldType.Bool),
                new Tuple<string, ParserPathElementFieldType>("int", ParserPathElementFieldType.Int),
                new Tuple<string, ParserPathElementFieldType>("kv", ParserPathElementFieldType.KeyValue)

                // If edited, make sure to update ParsePathFieldType
            };

            PathElementTypes = new Tuple<string, ParserPathElementType>[]
            {
                new Tuple<string, ParserPathElementType>("!", ParserPathElementType.NamedField),
                new Tuple<string, ParserPathElementType>("$", ParserPathElementType.FilterField),
                new Tuple<string, ParserPathElementType>("#", ParserPathElementType.IndexField)
            };
        }

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

        private static readonly TestCaseData[] CastKeyValueCases =
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
            DictionaryTestDataBuilder.TestAgainst("\"key=value;oh=boy\"=\"test=pain1\"").For("CastKeyValue(\"key=value;oh=boy\"=\"test=pain1\")").WhichWill().ReturnWith("key=value;oh=boy", "test=pain1"),
            DictionaryTestDataBuilder.TestAgainst("\"key=value;oh=boy\"=\"test=pain1\";yep=\"this=pain2\"").For("CastKeyValue(\"key=value;oh=boy\"=\"test=pain1\";yep=\"this=pain2\")").WhichWill().ReturnWith("key=value;oh=boy", "test=pain1").And("yep", "this=pain2"),
            DictionaryTestDataBuilder.TestAgainst("\"let's \\\"use=more\\\" quotes\"=\"oh boy\"").For("CastKeyValue(\"let's \\\"use=more\\\" quotes\"=\"oh boy\")").WhichWill().ReturnWith("let's \\\"use=more\\\" quotes", "oh boy") //XXX this still looks weird
        };

        [TestCaseSource("CastKeyValueCases")]
        public object CastKeyValue(string value)
        {
            return ParserUtil.CastField(value, ParserPathElementFieldType.KeyValue);
        }

        [TestCase(null, ExpectedResult = null)]
        [TestCase("", ExpectedResult = null)]
        [TestCase("!", ExpectedResult = null)]
        [TestCase("#", ExpectedResult = null)]
        [TestCase("$", ExpectedResult = null)]
        [TestCase("&", ExpectedResult = null)]
        [TestCase("&int", ExpectedResult = null)]
        [TestCase("/!", ExpectedResult = null)]
        [TestCase("/#", ExpectedResult = null)]
        [TestCase("/$", ExpectedResult = null)]
        [TestCase("/#cake", ExpectedResult = null)]
        [TestCase("/#NaN", ExpectedResult = null)]
        [TestCase("/#-1", ExpectedResult = null)]
        [TestCase("/@bob", ExpectedResult = null)] // Unknown field type
        public object ParsePathNull(string value)
        {
            return ParserUtil.ParsePath(value);
        }

        [Test]
        public void ParsePathEmpty()
        {
            Assert.That(ParserUtil.ParsePath("/"), Is.Empty);
        }

        [Test]
        public void ParsePathDirectNamedField([Values("!name", "!name&str", "!name&boolean", "!name&INT", "!name&kv", "!name&cookie")]string value)
        {
            var path = ParserUtil.ParsePath(value);
            Assert.That(path, Is.Not.Null.And.Length.EqualTo(1));
            Assert.That(path[0], Has.Property("Type").EqualTo(ParserPathElementType.DirectNamedField).And.Property("StringValue").EqualTo("name"));
            // FieldType is tested by ParsePathFieldType
        }

        [TestCase("!name", ExpectedResult = ParserPathElementFieldType.String)]
        [TestCase("!name&", ExpectedResult = ParserPathElementFieldType.String)]
        [TestCase("!name&   ", ExpectedResult = ParserPathElementFieldType.String)]
        [TestCase("!name&str", ExpectedResult = ParserPathElementFieldType.String)]
        [TestCase("!name&string", ExpectedResult = ParserPathElementFieldType.String)]
        [TestCase("!name&StRinG", ExpectedResult = ParserPathElementFieldType.String)]
        [TestCase("!name&bool", ExpectedResult = ParserPathElementFieldType.Bool)]
        [TestCase("!name&boolean", ExpectedResult = ParserPathElementFieldType.Bool)]
        [TestCase("!name&int", ExpectedResult = ParserPathElementFieldType.Int)]
        [TestCase("!name&INT", ExpectedResult = ParserPathElementFieldType.Int)]
        [TestCase("!name&kv", ExpectedResult = ParserPathElementFieldType.KeyValue)]
        [TestCase("!name&value&kv", ExpectedResult = ParserPathElementFieldType.KeyValue)]
        [TestCase("!name&kv&value", ExpectedResult = ParserPathElementFieldType.Unknown)]
        [TestCase("!name&cookie", ExpectedResult = ParserPathElementFieldType.Unknown)]
        public object ParsePathFieldType(string value)
        {
            var path = ParserUtil.ParsePath(value);
            Assert.That(path, Is.Not.Null.And.Length.EqualTo(1));
            return path[0].FieldType;
        }
        
        [Test]
        public void ParsePathIndexField([Random(0, 20, 5, Distinct = true)]int value, [Values("", "&int")]string type) // Max can be int.MaxValue, but if you have that many children within a log, you're doing something wrong.
        {
            var path = ParserUtil.ParsePath($"/#{value}{type}");
            Assert.That(path, Is.Not.Null.And.Length.EqualTo(1));
            Assert.That(path[0], Has.Property("Type").EqualTo(ParserPathElementType.IndexField).And.Property("IndexValue").EqualTo(value));
        }

        [Test]
        public void ParsePathFilterField([Values("/$name", "/$name&int")]string value)
        {
            var path = ParserUtil.ParsePath(value);
            Assert.That(path, Is.Not.Null.And.Length.EqualTo(1));
            Assert.That(path[0], Has.Property("Type").EqualTo(ParserPathElementType.FilterField).And.Property("StringValue").EqualTo("name"));
        }

        [Test]
        public void ParsePathNamedField([Values("/!name", "/!name&int")]string value)
        {
            var path = ParserUtil.ParsePath(value);
            Assert.That(path, Is.Not.Null.And.Length.EqualTo(1));
            Assert.That(path[0], Has.Property("Type").EqualTo(ParserPathElementType.NamedField).And.Property("StringValue").EqualTo("name"));
        }

        [Test]
        public void VariableTest()
        {
            Assert.That(PathFieldTypes.Length, Is.EqualTo(Enum.GetValues(typeof(ParserPathElementFieldType)).Length - 2)); // Don't count unknown and not-a-value
            Assert.That(PathFieldTypes[0], Is.EqualTo(new Tuple<string, ParserPathElementFieldType>("string", ParserPathElementFieldType.String)));

            Assert.That(PathElementTypes.Length, Is.EqualTo(Enum.GetValues(typeof(ParserPathElementType)).Length - 2)); // Don't count unknown and DirectNamedField (which is more for accessing the attributes)
        }

        [Test]
        public void ParsePath([Random(0, int.MaxValue, 20, Distinct = true)]int seed)
        {
            // Probably a bit more complex then desired, but it does make something...
            var rand = new Random(seed);
            var pathCount = rand.Next(2, 9);

            var pathBuilder = new StringBuilder();
            var expectedPath = new Tuple<ParserPathElementType, object>[pathCount];
            for (int i = 0; i < pathCount; i++)
            {
                var type = PathElementTypes[rand.Next(0, PathElementTypes.Length)];
                object value;

                if (type.Item2 == ParserPathElementType.IndexField)
                {
                    // Int
                    value = rand.Next();
                }
                else
                {
                    // String
                    var bytes = new byte[16];
                    rand.NextBytes(bytes);
                    value = new Guid(bytes).ToString();
                }

                pathBuilder.AppendFormat("/{0}{1}", type.Item1, value);
                expectedPath[i] = new Tuple<ParserPathElementType, object>(type.Item2, value);
            }

            var appendType = rand.Next(0, 100) < 50;
            var typeInfo = PathFieldTypes[rand.Next(0, PathFieldTypes.Length)];
            if (appendType)
            {
                pathBuilder.AppendFormat("&{0}", typeInfo.Item1);
            }
            else
            {
                typeInfo = PathFieldTypes[0]; // Get the string type, since it's default
            }

            // For debugging
            Console.WriteLine(pathBuilder);

            // Actual tests
            var path = ParserUtil.ParsePath(pathBuilder.ToString());
            Assert.That(path, Is.Not.Null.And.Length.EqualTo(pathCount));
            for (int i = 0; i < pathCount; i++)
            {
                var typeExpression = Has.Property("Type").EqualTo(expectedPath[i].Item1);
                var valueExpression = typeExpression.And.Property(expectedPath[i].Item1 == ParserPathElementType.IndexField ? "IndexValue" : "StringValue").EqualTo(expectedPath[i].Item2);

                NUnit.Framework.Constraints.IResolveConstraint finalExpression;
                if (i == (pathCount - 1))
                {
                    finalExpression = valueExpression.And.Property("FieldType").EqualTo(typeInfo.Item2);
                }
                else
                {
                    finalExpression = valueExpression.And.Property("FieldType").EqualTo(ParserPathElementFieldType.NotAValue);
                }

                Assert.That(path[i], finalExpression);
            }
        }
    }
}
