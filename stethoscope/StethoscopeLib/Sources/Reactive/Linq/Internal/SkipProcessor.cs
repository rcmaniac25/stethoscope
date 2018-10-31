using Stateless;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Stethoscope.Reactive.Linq.Internal
{
    internal class SkipProcessor
    {
        private class SkipVisitor<T> : ExpressionVisitor
        {
            private int depth = -1;
            private T state;

            public Func<MethodCallExpression, int, T, Func<Expression, Expression>, Expression> MethodVisitHandler { get; set; }

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

        private enum State
        {
            Uninitialized,
            Setup,
            MethodCollection,
            FindProcessingRange,
            VisitExpressionTree,
            CountAndRemoveSkips,
            Done
        }

        private enum Trigger
        {
            Done,
            Invoke,

            HasMethods,
            NoMethods
        }

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Expression> processTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<Expression>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<List<MethodCallExpression>> hasMethodsMethodList = new StateMachine<State, Trigger>.TriggerWithParameters<List<MethodCallExpression>>(Trigger.HasMethods);
        
        private Expression finalExpression;
        private int skipDepth;
        
        public (Expression, int?) Process(Expression expression)
        {
            var stateMachine = CreateStateMachine();
            stateMachine.Fire(processTrigger, expression);

            int? skipCount = null;
            if (skipDepth > 0)
            {
                skipCount = skipDepth;
            }
            return (finalExpression, skipCount);
        }

        private StateMachine<State, Trigger> CreateStateMachine()
        {
            var machine = new StateMachine<State, Trigger>(State.Uninitialized);

            var invokeMethodList= new StateMachine<State, Trigger>.TriggerWithParameters<List<MethodCallExpression>>(Trigger.Invoke);
            var doneMethodList = new StateMachine<State, Trigger>.TriggerWithParameters<List<MethodCallExpression>>(Trigger.Done);
            
            var priorState = State.Uninitialized;
            var skipVisitor = new SkipVisitor<List<MethodCallExpression>>();

            machine.Configure(State.Uninitialized)
                .Permit(Trigger.Invoke, State.Setup);

            machine.Configure(State.Setup)
                .OnEntryFrom(processTrigger, expression =>
                {
                    skipDepth = -1;
                    finalExpression = expression;
                    machine.Fire(doneMethodList, new List<MethodCallExpression>());
                })
                .PermitReentry(Trigger.Invoke)
                .Permit(Trigger.Done, State.MethodCollection);

            machine.Configure(State.MethodCollection)
                .OnEntryFrom(doneMethodList, (methodCalls, transition) =>
                {
                    if (transition.Source == State.VisitExpressionTree)
                    {
                        if (methodCalls.Count > 0)
                        {
                            machine.Fire(hasMethodsMethodList, methodCalls);
                        }
                        else
                        {
                            machine.Fire(Trigger.NoMethods);
                        }
                    }
                    else
                    {
                        skipVisitor.MethodVisitHandler = (mexp, d, methCalls, visit) =>
                        {
                            methCalls.Add(mexp);
                            visit(mexp.Arguments[0]);
                            return mexp;
                        };
                        priorState = State.MethodCollection;
                        machine.Fire(invokeMethodList, methodCalls);
                    }
                })
                .Permit(Trigger.Invoke, State.VisitExpressionTree)
                .Permit(Trigger.HasMethods, State.FindProcessingRange)
                .Permit(Trigger.NoMethods, State.Done);

            machine.Configure(State.FindProcessingRange)
                .OnEntryFrom(hasMethodsMethodList, methodCalls => FindProcessingRange(methodCalls, machine))
                .Permit(Trigger.HasMethods, State.CountAndRemoveSkips)
                .Permit(Trigger.NoMethods, State.Done);

            machine.Configure(State.CountAndRemoveSkips)
                .OnEntryFrom(doneMethodList, (_, transition) =>
                {
                    if (transition.Source == State.VisitExpressionTree)
                    {
                        machine.Fire(Trigger.Done);
                    }
                })
                .OnEntryFrom(hasMethodsMethodList, methodCalls =>
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
                        skipVisitor.MethodVisitHandler = VisitExpressionToCountAndModify;
                        priorState = State.CountAndRemoveSkips;
                        machine.Fire(invokeMethodList, methodCalls);
                    }
                    else
                    {
                        machine.Fire(Trigger.Done);
                    }
                })
                .Permit(Trigger.Invoke, State.VisitExpressionTree)
                .Permit(Trigger.Done, State.Done);

            machine.Configure(State.VisitExpressionTree)
                .OnEntryFrom(invokeMethodList, methods =>
                {
                    finalExpression = skipVisitor.Visit(finalExpression, methods);
                    machine.Fire(doneMethodList, methods);
                })
                .OnExit(() => priorState = State.Uninitialized)
                .PermitDynamic(Trigger.Done, () => priorState);

            machine.Configure(State.Done)
                .PermitIf(processTrigger, State.Setup);

            return machine;
        }

        private void FindProcessingRange(List<MethodCallExpression> methodCalls, StateMachine<State, Trigger> stateMachine)
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
                stateMachine.Fire(hasMethodsMethodList, methodCalls);
            }
            else
            {
                stateMachine.Fire(Trigger.NoMethods);
            }
        }

        private Expression VisitExpressionToCountAndModify<T>(MethodCallExpression expression, int depth, T state, Func<Expression, Expression> visit)
        {
            var child = visit(expression.Arguments[0]);
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
