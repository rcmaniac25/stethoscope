using NUnit.Framework;

namespace Stethoscope.Tests.Helpers
{
    public static class TestDataBuilderExtensions
    {
        public static TestCaseData Which(this TestDataBuilder builder)
        {
            return builder.Build();
        }

        public static T For<T>(this T builder, string testName) where T : TestDataBuilder
        {
            builder.InternalTestData = builder.InternalTestData.SetName(testName);
            return builder;
        }
    }
}
