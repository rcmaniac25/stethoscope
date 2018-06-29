using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Stethoscope.Tests.Helpers
{
    public static class Util
    {
        // Somehow, something like this doesn't exist in C#. Pretty sure one exists in F#
        public static IEnumerable<T> ConcatMany<T>(IEnumerable<IEnumerable<T>> enumerables)
        {
            foreach (var en in enumerables)
            {
                foreach (var value in en)
                {
                    yield return value;
                }
            }
        }

        public static IObservable<object> CastGenericObservable<TActual>(TActual obs)
        {
            return CastGenericObservable(obs, typeof(TActual));
        }

        public static IObservable<object> CastGenericObservable(object expectedObservable, Type expectedObservableType)
        {
            if (expectedObservableType == null)
            {
                throw new ArgumentNullException(nameof(expectedObservableType));
            }
            if (expectedObservable == null)
            {
                return null;
            }
            if (expectedObservableType.IsGenericType &&
                (expectedObservableType.GetGenericTypeDefinition() == typeof(IObservable<>) || expectedObservableType.GetGenericTypeDefinition() == typeof(IQbservable<>)))
            {
                var castGeneric = typeof(Observable).GetMethod("Cast");
                var cast = castGeneric.MakeGenericMethod(expectedObservableType.GetGenericArguments());
                return (IObservable<object>)cast.Invoke(null, new object[] { expectedObservable });
            }
            throw new ArgumentException("Argument is not of type IObservable", nameof(expectedObservableType));
        }
    }
}
