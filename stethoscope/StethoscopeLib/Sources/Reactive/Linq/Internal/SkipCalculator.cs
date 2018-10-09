using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Stethoscope.Reactive.Linq.Internal
{
    internal class SkipCalculator : ExpressionVisitor
    {
        private int skipDepth;

        public int? CalculateSkip(Expression expression)
        {
            skipDepth = -1;

            Visit(expression);

            if (skipDepth <= 0)
            {
                return null;
            }
            return skipDepth;
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            if (expression.Method.Name == "Skip")
            {
                if (skipDepth < 0)
                {
                    skipDepth = 0;
                }
                skipDepth += ExpressionTreeHelpers.GetValueFromExpression<int>(expression.Arguments[1]);
            }

            Visit(expression.Arguments[0]);

            return expression;
        }
    }
}
