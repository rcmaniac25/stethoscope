using NUnit.Framework.Constraints;

using Stethoscope.Tests.Helpers;

using System.Reactive.Linq;

namespace Stethoscope.Tests.Constraints
{
    public class EmptyObservableConstraint : Constraint
    {
        public override string Description { get => "empty IObservable"; }

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            var obs = Util.CastGenericObservable(actual);
            return new ConstraintResult(this, actual, obs.IsEmpty().Wait());
        }
    }
}
