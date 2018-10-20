using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Stethoscope.Reactive.Linq.Internal
{
    internal class SkipCalculator : ExpressionVisitor
    {
        private enum VisitStage
        {
            Uninitialized,

            Stage1,
            Stage2,
            Stage3,
            Done
        }

        private int skipDepth;
        private VisitStage stage = VisitStage.Uninitialized;
        private List<MethodCallExpression> methodCalls;

        public int? CalculateSkip(Expression expression)
        {
            if (stage != VisitStage.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(CalculateSkip)} is already doing a calculation. Create a new instance for each calculation you want to perform.");
            }
            Reset();

            while (stage != VisitStage.Done)
            {
                ExecuteStages(expression);
            }

            stage = VisitStage.Uninitialized;

            if (skipDepth <= 0)
            {
                return null;
            }
            return skipDepth;
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
            Visit(expression);
        }

        private Expression VisitMethodCallStage1(MethodCallExpression expression)
        {
            methodCalls.Add(expression);
            Visit(expression.Arguments[0]);
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
        }
    }
}
