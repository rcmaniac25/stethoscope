using Stateless;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Stethoscope.Reactive.Linq.Internal
{
    internal class SkipProcessor
    {
        #region SMState

        private struct SMState : ICloneable
        {
            public static SMState InvalidState = new SMState() { SkipDepth = -1 };

            public Expression Expression { get; private set; }
            public int SkipDepth { get; private set; }

            public List<MethodCallExpression> MethodCalls { get; private set; }
            public State PriorState { get; private set; }
            public ExpressionMethodVisitor<List<MethodCallExpression>> SkipVisitor { get; private set; }

            public StateMachine<State, Trigger> StateMachine { get; private set; }
            public Action<SMState> DoneHandler { get; private set; }

            public SMState(Expression expression, StateMachine<State, Trigger> stateMachine, Action<SMState> doneHandler)
            {
                Expression = expression;
                SkipDepth = -1;

                MethodCalls = new List<MethodCallExpression>();
                PriorState = State.Uninitialized;
                SkipVisitor = new ExpressionMethodVisitor<List<MethodCallExpression>>();

                StateMachine = stateMachine;
                DoneHandler = doneHandler;
            }

            public SMState SetExpression(Expression expression)
            {
                var clone = (SMState)Clone();
                clone.Expression = expression;
                return clone;
            }

            public SMState SetSkipDepth(int skipDepth)
            {
                var clone = (SMState)Clone();
                clone.SkipDepth = skipDepth;
                return clone;
            }

            public SMState SetPriorState(State priorState)
            {
                var clone = (SMState)Clone();
                clone.PriorState = priorState;
                return clone;
            }

            public object Clone()
            {
                return new SMState()
                {
                    Expression = Expression,
                    SkipDepth = SkipDepth,

                    MethodCalls = MethodCalls,
                    PriorState = PriorState,
                    SkipVisitor = SkipVisitor,

                    StateMachine = StateMachine,
                    DoneHandler = DoneHandler
                };
            }
        }

        #endregion

        #region States and Triggers

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

        #endregion

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<(Expression, Action<SMState>)> processTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<(Expression, Action<SMState>)>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> hasMethodsTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.HasMethods);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> invokeTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> doneTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Done);
        
        public (Expression expression, int? skipDepth) Process(Expression expression)
        {
            var finishedState = SMState.InvalidState;
            void ProcessIsComplete(SMState state)
            {
                finishedState = state;
            }

            var stateMachine = CreateStateMachine();
            stateMachine.Fire(processTrigger, (expression, ProcessIsComplete));
            
            if (finishedState.SkipDepth > 0)
            {
                return (finishedState.Expression, finishedState.SkipDepth);
            }
            return (expression, null);
        }

        #region Create State Machine

        private StateMachine<State, Trigger> CreateStateMachine()
        {
            var machine = new StateMachine<State, Trigger>(State.Uninitialized);
            
            machine.Configure(State.Uninitialized)
                .Permit(Trigger.Invoke, State.Setup);

            machine.Configure(State.Setup)
                .OnEntryFrom(processTrigger, ((Expression expression, Action<SMState> doneHandler) processParameters) =>
                {
                    var internalState = new SMState(processParameters.expression, machine, processParameters.doneHandler);
                    machine.Fire(doneTrigger, internalState);
                })
                .PermitReentry(Trigger.Invoke)
                .Permit(Trigger.Done, State.MethodCollection);

            machine.Configure(State.MethodCollection)
                .OnEntryFrom(doneTrigger, (state, transition) =>
                {
                    if (transition.Source == State.VisitExpressionTree)
                    {
                        DoneCollectingMethods(state);
                    }
                    else
                    {
                        CollectMethods(state);
                    }
                })
                .Permit(Trigger.Invoke, State.VisitExpressionTree)
                .Permit(Trigger.HasMethods, State.FindProcessingRange)
                .Permit(Trigger.NoMethods, State.Done);

            machine.Configure(State.FindProcessingRange)
                .OnEntryFrom(hasMethodsTrigger, FindProcessingRange)
                .Permit(Trigger.HasMethods, State.CountAndRemoveSkips)
                .Permit(Trigger.NoMethods, State.Done);

            machine.Configure(State.CountAndRemoveSkips)
                .OnEntryFrom(doneTrigger, (state, transition) =>
                {
                    if (transition.Source == State.VisitExpressionTree)
                    {
                        machine.Fire(doneTrigger, state);
                    }
                })
                .OnEntryFrom(hasMethodsTrigger, CountAndRemoveSkips)
                .Permit(Trigger.Invoke, State.VisitExpressionTree)
                .Permit(Trigger.Done, State.Done);

            machine.Configure(State.VisitExpressionTree)
                .OnEntryFrom(invokeTrigger, state =>
                {
                    var expression = state.SkipVisitor.Visit(state.Expression, state.MethodCalls);
                    machine.Fire(doneTrigger, state.SetExpression(expression));
                })
                .PermitDynamic(doneTrigger, state => state.PriorState);

            machine.Configure(State.Done)
                .OnEntryFrom(doneTrigger, state => state.DoneHandler(state))
                .PermitIf(processTrigger, State.Setup);

            return machine;
        }

        #endregion

        #region State Entry Operations

        private void CollectMethods(SMState state)
        {
            state.SkipVisitor.MethodVisitHandler = (mexp, d, methCalls, visit) =>
            {
                methCalls.Add(mexp);
                visit(mexp.Arguments[0]);
                return mexp;
            };
            state.StateMachine.Fire(invokeTrigger, state.SetPriorState(State.MethodCollection));
        }

        private void DoneCollectingMethods(SMState state)
        {
            if (state.MethodCalls.Count > 0)
            {
                state.StateMachine.Fire(hasMethodsTrigger, state);
            }
            else
            {
                state.StateMachine.Fire(Trigger.NoMethods);
            }
        }

        private void FindProcessingRange(SMState state)
        {
            int? countSkipsFrom = null; // null means don't process, = MethodCalls.Count means skip everything so don't process, else we want to process
            for (int i = 0; i < state.MethodCalls.Count; i++)
            {
                var method = state.MethodCalls[i];
                if (method.Method.Name == "Skip")
                {
                    if (method.Arguments[1].Type != typeof(int))
                    {
                        countSkipsFrom = i + 1;
                    }
                    else if (!countSkipsFrom.HasValue)
                    {
                        // Do this to ensure it's known there _are_ Skips that can be processed.
                        countSkipsFrom = 0;
                    }
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
            if (countSkipsFrom.HasValue && countSkipsFrom < state.MethodCalls.Count)
            {
                for (int i = 0; i < countSkipsFrom; i++)
                {
                    state.MethodCalls[i] = null;
                }
                state.StateMachine.Fire(hasMethodsTrigger, state);
            }
            else
            {
                state.StateMachine.Fire(Trigger.NoMethods);
            }
        }

        private void CountAndRemoveSkips(SMState state)
        {
            var skipDepth = -1;
            foreach (var method in state.MethodCalls)
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
            if (skipDepth >= 0)
            {
                state.SkipVisitor.MethodVisitHandler = VisitExpressionToCountAndModify;
                state.StateMachine.Fire(invokeTrigger, state.SetPriorState(State.CountAndRemoveSkips).SetSkipDepth(skipDepth));
            }
            else
            {
                state.StateMachine.Fire(doneTrigger, state);
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

        #endregion
    }
}
