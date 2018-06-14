using NUnit.Framework.Constraints;

using System;

namespace Stethoscope.Tests.Constraints
{
    // Extended version of EmptyConstraint
    public class ExtendedEmptyConstraint : Constraint
    {
        private Constraint realConstraint;
        private EmptyConstraint empty;

        public override string Description { get => realConstraint == null ? "<empty>" : realConstraint.Description; }

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            if (actual != null)
            {
                if (typeof(TActual).IsGenericType && typeof(TActual).GetGenericTypeDefinition() == typeof(IObservable<>))
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
