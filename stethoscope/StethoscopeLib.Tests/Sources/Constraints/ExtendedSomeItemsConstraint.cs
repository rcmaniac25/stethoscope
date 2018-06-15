using NUnit.Framework.Constraints;

using Stethoscope.Tests.Helpers;

using System.Collections;
using System.Reactive.Linq;

namespace Stethoscope.Tests.Constraints
{
    public class ExtendedSomeItemsConstraint : SomeItemsConstraint
    {
        public ExtendedSomeItemsConstraint(IConstraint itemConstraint) : base(itemConstraint)
        {
        }

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            if (actual == null)
            {
                return base.ApplyTo(actual);
            }

            if (actual is IEnumerable)
            {
                return base.ApplyTo(actual);
            }
            var observable = Util.CastGenericObservable(actual);
            return base.ApplyTo(observable.ToEnumerable());
        }
    }
}
