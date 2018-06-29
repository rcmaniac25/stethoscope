using NUnit.Framework.Constraints;

using System;
using System.Reactive.Linq;

namespace Stethoscope.Tests.Constraints
{
    public class ExtendedEmptyConstraint : Constraint
    {
        private Constraint realConstraint;
        private EmptyConstraint empty;

        public override string Description { get => realConstraint == null ? "<empty>" : realConstraint.Description; }

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            if (actual != null)
            {
                if (typeof(TActual).IsGenericType &&
                    (typeof(TActual).GetGenericTypeDefinition() == typeof(IObservable<>) || typeof(TActual).GetGenericTypeDefinition() == typeof(IQbservable<>)))
                {
                    realConstraint = new EmptyObservableConstraint();
                }
            }
            if (realConstraint != null)
            {
                return realConstraint.ApplyTo(actual);
            }

            if (empty == null)
            {
                empty = new EmptyConstraint();
            }
            return empty.ApplyTo(actual);
        }
    }
}
