using NUnit.Framework;

namespace Stethoscope.Tests.Helpers
{
    public abstract class TestDataBuilder
    {
        protected TestCaseData testData;

        protected TestDataBuilder(TestCaseData testData)
        {
            this.testData = testData;
        }

        public virtual TestCaseData Build()
        {
            return testData;
        }

        // For extension functions
        internal TestCaseData InternalTestData
        {
            get
            {
                return testData;
            }
            set
            {
                testData = value;
            }
        }
    }
}
