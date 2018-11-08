using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Stethoscope.Reactive.Linq.Internal
{
    internal class SkipProcessor : ExpressionVisitor
    {
        private enum VisitStage
        {
            Uninitialized,

            Stage1,
            Stage2,
            Stage3,
            Done
        }

        private Expression finalExpression;
        private int skipDepth;
        private VisitStage stage = VisitStage.Uninitialized;
        private List<MethodCallExpression> methodCalls;

        public int? SkipCount
        {
            get
            {
                if (skipDepth <= 0)
                {
                    return null;
                }
                return skipDepth;
            }
        }

        public Expression Process(Expression expression)
        {
            if (stage != VisitStage.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(Process)} is already doing a calculation. Create a new instance for each concurrent operation you want to perform.");
            }
            Reset();
            finalExpression = expression;

            while (stage != VisitStage.Done)
            {
                ExecuteStages(expression);
            }

            stage = VisitStage.Uninitialized;

            return finalExpression;
        }

        private void Reset()
        {
            stage = VisitStage.Stage1;
            skipDepth = -1;
            methodCalls = new List<MethodCallExpression>();
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            if (stage == VisitStage.Stage1)
            {
                return VisitMethodCallStage1(expression);
            }
            else if (stage == VisitStage.Stage3)
            {
                return VisitMethodCallStage3(expression);
            }

            throw new InvalidOperationException($"Unknown stage: {stage}");
        }

        private void ExecuteStages(Expression expression)
        {
            if (stage == VisitStage.Stage1)
            {
                Stage1(expression);
                if (methodCalls.Count > 0)
                {
                    stage = VisitStage.Stage2;
                }
                else
                {
                    stage = VisitStage.Done;
                }
            }
            else if (stage == VisitStage.Stage2)
            {
                Stage2(expression);
                if (methodCalls.Count > 0)
                {
                    stage = VisitStage.Stage3;
                }
                else
                {
                    stage = VisitStage.Done;
                }
            }
            else if (stage == VisitStage.Stage3)
            {
                Stage3(expression);
                stage = VisitStage.Done;
            }
            else
            {
                throw new InvalidOperationException($"Unknown stage: {stage}");
            }
        }

        private void Stage1(Expression expression)
        {
            base.Visit(expression);
        }

        private Expression VisitMethodCallStage1(MethodCallExpression expression)
        {
            methodCalls.Add(expression);
            base.Visit(expression.Arguments[0]);
            return expression;
        }

        private void Stage2(Expression expression)
        {
            int countSkipsFrom = 0;
            for (int i = 0; i < methodCalls.Count; i++)
            {
                var method = methodCalls[i];
                if (method.Method.Name == "Skip" && method.Arguments[1].Type != typeof(int))
                {
                    countSkipsFrom = i + 1;
                }
                else if (method.Arguments.Count > 1)
                {
                    for (int a = 1; a < method.Arguments.Count; a++)
                    {
                        if (method.Arguments[a].NodeType == ExpressionType.Lambda || method.Arguments[a].NodeType == ExpressionType.Quote)
                        {
                            countSkipsFrom = i + 1;
                            break;
                        }
                    }
                }
            }
            if (countSkipsFrom < methodCalls.Count)
            {
                for (int i = 0; i < countSkipsFrom; i++)
                {
                    methodCalls[i] = null;
                }
            }
            else
            {
                methodCalls.Clear();
            }
        }

        private void Stage3(Expression expression)
        {
            foreach (var method in methodCalls)
            {
                if (method != null && method.Method.Name == "Skip" && method.Arguments[1].Type == typeof(int))
                {
                    if (skipDepth < 0)
                    {
                        skipDepth = 0;
                    }
                    skipDepth += ExpressionTreeHelpers.GetValueFromExpression<int>(method.Arguments[1]);
                }
            }
            if (skipDepth > 0)
            {
                finalExpression = base.Visit(expression);
            }
        }

        private Expression VisitMethodCallStage3(MethodCallExpression expression)
        {
            var child = base.Visit(expression.Arguments[0]);
            if (expression.Method.Name == "Skip" && expression.Arguments[1].Type == typeof(int))
            {
                return child;
            }
            if (child != expression.Arguments[0])
            {
                return Expression.Call(expression.Method, new Expression[] { child }.Concat(expression.Arguments.Skip(1)));
            }
            return expression;
        }
    }
}
