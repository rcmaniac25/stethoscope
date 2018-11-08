﻿using System.Linq.Expressions;

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
                // Only specific skips can be used
                if (expression.Arguments[1].Type == typeof(int))
                {
                    if (skipDepth < 0)
                    {
                        skipDepth = 0;
                    }
                    skipDepth += ExpressionTreeHelpers.GetValueFromExpression<int>(expression.Arguments[1]);
                }
                else if (skipDepth >= 0)
                {
                    skipDepth = -1;
                }
            }
            else if (skipDepth >= 0 && expression.Arguments.Count > 1)
            {
                // If any function takes a lambda, there's some programatic element that can't skip a specific amount
                for (int i = 1; i < expression.Arguments.Count; i++)
                {
                    if (expression.Arguments[i].NodeType == ExpressionType.Lambda || expression.Arguments[i].NodeType == ExpressionType.Quote)
                    {
                        skipDepth = -1;
                        break;
                    }
                }
            }

            Visit(expression.Arguments[0]);

            return expression;
        }
    }
}
