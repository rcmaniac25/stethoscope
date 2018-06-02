using System.Collections.Generic;

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
    }
}
