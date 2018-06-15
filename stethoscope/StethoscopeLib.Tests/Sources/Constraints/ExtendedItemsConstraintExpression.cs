using NUnit.Framework.Constraints;

using System;

namespace Stethoscope.Tests.Constraints
{
    public sealed class ExtendedItemsConstraintExpression : ConstraintExpression
    {
        private ResolvableConstraintExpression resolvableExpression;

        public ExtendedItemsConstraintExpression(ResolvableConstraintExpression resolvableConstraintExpression)
        {
            resolvableExpression = resolvableConstraintExpression;
        }

        public ResolvableConstraintExpression Items
        {
            get
            {
                // Since we can't create an expression, do this in case any weird builder stack stuff is done.
                if (resolvableExpression == null)
                {
                    throw new InvalidOperationException("Can only be used once");
                }
                var expression = resolvableExpression;
                resolvableExpression = null;
                return expression;
            }
        }
    }
}
