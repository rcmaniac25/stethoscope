using NUnit.Framework.Constraints;

using Stethoscope.Tests.Constraints;

namespace Stethoscope.Tests.Helpers
{
    public class IsEx : NUnit.Framework.Is
    {
        public static ExtendedEmptyConstraint ExEmpty => new ExtendedEmptyConstraint();
    }

    public static class NUnitExtensions
    {
        public static ExtendedEmptyConstraint ExEmpty(this ConstraintExpression expression)
        {
            var constraint = new ExtendedEmptyConstraint();
            expression.Append(constraint);
            return constraint;
        }
    }
}
