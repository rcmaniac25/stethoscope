using NUnit.Framework.Constraints;

using Stethoscope.Tests.Constraints;

using System;
using System.Collections;
using System.Reactive.Linq;

namespace Stethoscope.Tests.Helpers
{
    public class IsEx : NUnit.Framework.Is
    {
        public static ExtendedEmptyConstraint ExEmpty => new ExtendedEmptyConstraint();
        
        public static ExtendedCollectionSubsetConstraint ExSubsetOf(IEnumerable expected)
        {
            return new ExtendedCollectionSubsetConstraint(expected);
        }

        public static ExtendedCollectionSubsetConstraint ExSubsetOf<T>(IObservable<T> expected)
        {
            return new ExtendedCollectionSubsetConstraint(expected.ToEnumerable());
        }
    }
    
    public class HasEx : NUnit.Framework.Has
    {
        public static ExtendedItemsConstraintExpression ExOne => new ConstraintExpression().ExExactly(1);

        public static ExtendedItemsConstraintExpression ExExactly(int expectedCount)
        {
            return new ConstraintExpression().ExExactly(expectedCount);
        }

        public static ExtendedSomeItemsConstraint ExMember(object expected)
        {
            return new ExtendedSomeItemsConstraint(new EqualConstraint(expected));
        }
    }

    public static class NUnitExtensions
    {
        public static ExtendedEmptyConstraint ExEmpty(this ConstraintExpression expression)
        {
            var constraint = new ExtendedEmptyConstraint();
            expression.Append(constraint);
            return constraint;
        }

        public static ExtendedCollectionSubsetConstraint ExSubsetOf(this ConstraintExpression expression, IEnumerable expected)
        {
            var constraint = new ExtendedCollectionSubsetConstraint(expected);
            expression.Append(constraint);
            return constraint;
        }

        public static ExtendedCollectionSubsetConstraint ExSubsetOf<T>(this ConstraintExpression expression, IObservable<T> expected)
        {
            var constraint = new ExtendedCollectionSubsetConstraint(expected.ToEnumerable());
            expression.Append(constraint);
            return constraint;
        }
        
        public static ExtendedItemsConstraintExpression ExExactly(this ConstraintExpression expression, int expectedCount)
        {
            return new ExtendedItemsConstraintExpression(expression.Append(new ExtendedExactCountOperator(expectedCount)));
        }

        public static ExtendedItemsConstraintExpression ExOne(this ConstraintExpression expression)
        {
            return new ExtendedItemsConstraintExpression(expression.Append(new ExtendedExactCountOperator(1)));
        }

        public static ExtendedSomeItemsConstraint ExMember(this ConstraintExpression expression, object expected)
        {
            return (ExtendedSomeItemsConstraint)expression.Append(new ExtendedSomeItemsConstraint(new EqualConstraint(expected)));
        }

        public static ExtendedSomeItemsConstraint ExContains(this ConstraintExpression expression, object expected)
        {
            return (ExtendedSomeItemsConstraint)expression.Append(new ExtendedSomeItemsConstraint(new EqualConstraint(expected)));
        }

        public static ExtendedSomeItemsConstraint ExContain(this ConstraintExpression expression, object expected)
        {
            return expression.ExContains(expected);
        }
    }
}
