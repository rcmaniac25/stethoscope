using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

namespace Stethoscope.Reactive.Linq.Internal
{
    internal class SkipTreeModifier : ExpressionVisitor
    {
        private Stack<MethodCallExpression> expressionsToSave;

        public override Expression Visit(Expression node)
        {
            expressionsToSave = null;
            return base.Visit(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            var returnChild = false;
            var expressionsToSaveOriginallyExisted = expressionsToSave != null; //TODO

            if (expression.Method.Name == "Skip")
            {
                // Only specific skips can be used
                if (expression.Arguments[1].Type == typeof(int))
                {
                    returnChild = true;
                    if (expressionsToSave == null)
                    {
                        expressionsToSave = new Stack<MethodCallExpression>();
                        expressionsToSaveOriginallyExisted = true;
                    }
                }
                else if (expressionsToSave != null)
                {
                    expressionsToSave = null;
                }
            }
            else if (expressionsToSave != null && expression.Arguments.Count > 1)
            {
                // If any function takes a lambda, there's some programatic element that can't skip a specific amount
                for (int i = 1; i < expression.Arguments.Count; i++)
                {
                    if (expression.Arguments[i].NodeType == ExpressionType.Lambda || expression.Arguments[i].NodeType == ExpressionType.Quote)
                    {
                        expressionsToSave = null;
                        break;
                    }
                }
            }

            if (expressionsToSave != null)
            {
                if (returnChild)
                {
                    expressionsToSave.Push(null);
                }
                else
                {
                    expressionsToSave.Push(expression);
                }
            }

            var child = base.Visit(expression.Arguments[0]);

            if (expressionsToSave != null)
            {
                if (expressionsToSave.Count > 0)
                {
                    var exp = expressionsToSave.Pop();
                    if (exp != null)
                    {
                        return exp;
                    }
                }
                if (expressionsToSaveOriginallyExisted)
                {
                    return child;
                }
                return Expression.Call(expression.Method, new Expression[] { child }.Concat(expression.Arguments.Skip(1)));
            }
            return expression;
        }
    }
}
