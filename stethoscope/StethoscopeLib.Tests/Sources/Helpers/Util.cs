using System;
using System.Collections.Generic;
using System.Linq;
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
                if (expectedObservableType.GenericTypeArguments[0].IsValueType)
                {
                    // ValueTypes need to be cast, but the existing Cast function doesn't support ValueTypes, so we need to do the work ourselves
                    var selectGeneric = typeof(Observable).GetMethods().First(m => m.Name == "Select" && m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);
                    var select = selectGeneric.MakeGenericMethod(expectedObservableType.GenericTypeArguments[0], typeof(object));
                    var castGeneric = typeof(Util).GetMethod(nameof(GenericObservableCast), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    var cast = castGeneric.MakeGenericMethod(expectedObservableType.GenericTypeArguments);
                    var funcCast = typeof(Func<>).Assembly.DefinedTypes.First(t => t.Name == "Func`2" && t.GenericTypeParameters.Length == 2).MakeGenericType(expectedObservableType.GenericTypeArguments[0], typeof(object));
                    var castDelegate = Delegate.CreateDelegate(funcCast, cast);
                    return (IObservable<object>)select.Invoke(null, new object[] { expectedObservable, castDelegate });
                }
                else
                {
                    // Non-ValueTypes can just use the cast function
                    var castGeneric = typeof(Observable).GetMethod("Cast");
                    var cast = castGeneric.MakeGenericMethod(expectedObservableType.GenericTypeArguments);
                    return (IObservable<object>)cast.Invoke(null, new object[] { expectedObservable });
                }
            }
            throw new ArgumentException("Argument is not of type IObservable", nameof(expectedObservableType));
        }

        private static object GenericObservableCast<T>(T value) where T : struct
        {
            return value;
        }
    }
}
