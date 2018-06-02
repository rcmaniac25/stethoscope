using NUnit.Framework;

using System.Collections.Generic;

namespace Stethoscope.Tests.Helpers
{
    public class DictionaryTestDataBuilder : TestDataBuilder
    {
        private Dictionary<string, string> returnData;

        private DictionaryTestDataBuilder(TestCaseData testData) : base(testData)
        {
            returnData = new Dictionary<string, string>();
        }

        public static DictionaryTestDataBuilder TestAgainst(string data)
        {
            return new DictionaryTestDataBuilder(new TestCaseData(data));
        }

        public DictionaryTestDataBuilder AddResultKV(string key, string value)
        {
            returnData.Add(key, value);
            return this;
        }

        public override TestCaseData Build()
        {
            var testCase = base.Build();
            testCase.ExpectedResult = returnData;
            return testCase;
        }

        // Syntax nice-ify
        public DictionaryTestDataBuilder WhichWill()
        {
            return this;
        }

        public DictionaryTestDataBuilder ReturnWith(string key, string value)
        {
            return AddResultKV(key, value);
        }

        public DictionaryTestDataBuilder And(string key, string value)
        {
            return AddResultKV(key, value);
        }

        public static implicit operator TestCaseData(DictionaryTestDataBuilder builder)
        {
            return builder.Build();
        }
    }
}
