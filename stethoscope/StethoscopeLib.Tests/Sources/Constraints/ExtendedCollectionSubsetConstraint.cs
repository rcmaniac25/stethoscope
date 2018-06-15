using NUnit.Framework.Constraints;

using Stethoscope.Tests.Helpers;

using System.Collections;
using System.Reactive.Linq;

namespace Stethoscope.Tests.Constraints
{
    public class ExtendedCollectionSubsetConstraint : CollectionSubsetConstraint
    {
        public ExtendedCollectionSubsetConstraint(IEnumerable expected) : base(expected)
        {
        }

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            if (actual == null)
            {
                return base.ApplyTo(actual);
            }

            IEnumerable enumerable;
            if (actual is IEnumerable en)
            {
                enumerable = en;
            }
            else
            {
                var observable = Util.CastGenericObservable(actual);
                enumerable = observable.ToEnumerable();
            }
            return base.ApplyTo(enumerable);
        }
    }
}
