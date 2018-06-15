using NUnit.Framework.Constraints;

namespace Stethoscope.Tests.Constraints
{
    // Copy of ExactCountOperator with a change in reduce (the expectedCount value is private, so I needed to reimplement)
    public class ExtendedExactCountOperator : SelfResolvingOperator
    {
        private readonly int expectedCount;

        public ExtendedExactCountOperator(int expectedCount)
        {
            // Collection Operators stack on everything
            // and allow all other ops to stack on them
            this.left_precedence = 1;
            this.right_precedence = 10;

            this.expectedCount = expectedCount;
        }

        public override void Reduce(ConstraintBuilder.ConstraintStack stack)
        {
            if (RightContext == null || RightContext is BinaryOperator)
                stack.Push(new ExtendedExactCountConstraint(expectedCount));
            else
                stack.Push(new ExtendedExactCountConstraint(expectedCount, stack.Pop()));
        }
    }
}
