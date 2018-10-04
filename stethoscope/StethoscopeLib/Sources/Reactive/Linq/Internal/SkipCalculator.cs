using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Stethoscope.Reactive.Linq.Internal
{
    internal class SkipCalculator : ExpressionVisitor
    {
        private int skipDepth;
        private bool processSkips;

        public int? CalculateSkip(Expression expression)
        {
            skipDepth = -1;
            processSkips = true;

            Visit(expression);

            if (skipDepth <= 0)
            {
                return null;
            }
            return skipDepth;
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            if (processSkips)
            {
                if (expression.Method.Name == "Skip")
                {
                    if (skipDepth < 0)
                    {
                        skipDepth = 0;
                    }
                    //TODO
                }

                Visit(expression.Arguments[0]);
            }

            return expression;
        }
    }
}
