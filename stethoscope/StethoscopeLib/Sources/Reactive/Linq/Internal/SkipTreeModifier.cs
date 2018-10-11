using System.Linq.Expressions;

namespace Stethoscope.Reactive.Linq.Internal
{
    internal class SkipTreeModifier : ExpressionVisitor
    {
        private bool hasDepth;

        public override Expression Visit(Expression node)
        {
            hasDepth = false;
            var res = base.Visit(node);
            return res;
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            var returnChild = false;
            var originalHasDepth = hasDepth;

            if (expression.Method.Name == "Skip")
            {
                // Only specific skips can be used
                if (expression.Arguments[1].Type == typeof(int))
                {
                    returnChild = true;
                    hasDepth = true;
                }
            }
            else if (hasDepth && expression.Arguments.Count > 1)
            {
                // If any function takes a lambda, there's some programatic element that can't skip a specific amount
                for (int i = 1; i < expression.Arguments.Count; i++)
                {
                    if (expression.Arguments[i].NodeType == ExpressionType.Lambda)
                    {
                        hasDepth = false;
                        break;
                    }
                }
            }

            var child = base.Visit(expression.Arguments[0]);

            return returnChild && (hasDepth == originalHasDepth) ? child : expression;
        }
    }
}
