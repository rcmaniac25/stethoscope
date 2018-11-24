using System;
using System.Linq.Expressions;

namespace Stethoscope.Reactive.Linq.Internal
{
    /// <summary>
    /// An expression visitor implementation that invokes a callback for method calls.
    /// </summary>
    /// <typeparam name="T">The type of state to be provided to the callback.</typeparam>
    public class ExpressionMethodVisitor<T> : ExpressionVisitor
    {
        private int depth = -1;
        private T state;

        /// <summary>
        /// Get or set a handler for method calls. First argument is the method call, second is the expression depth, third is state, forth is a Func to visit a tree of the expression. Return the input expression, or any desired changes to the tree.
        /// </summary>
        public Func<MethodCallExpression, int, T, Func<Expression, Expression>, Expression> MethodVisitHandler { get; set; }

        /// <summary>
        /// Visit an expression tree.
        /// </summary>
        /// <param name="node">The specific node of the tree to visit.</param>
        /// <param name="state">The state to pass to the handler.</param>
        /// <returns>The processed expression tree.</returns>
        public Expression Visit(Expression node, T state)
        {
            if (depth >= 0)
            {
                throw new InvalidOperationException("Visitor already in use");
            }

            this.state = state;
            depth = -1;
            var res = base.Visit(node);
            depth = -1;

            return res;
        }

        /// <summary>
        /// Visit a specific method call.
        /// </summary>
        /// <param name="expression">Method call that has been processed.</param>
        /// <returns>The processed method call.</returns>
        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            Expression result = expression;
            depth++;
            if (MethodVisitHandler != null)
            {
                result = MethodVisitHandler(expression, depth, state, base.Visit);
            }
            depth--;
            return result;
        }
    }
}
