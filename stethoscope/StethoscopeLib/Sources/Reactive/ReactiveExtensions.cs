using System;
using System.Collections.Generic;

namespace Stethoscope.Reactive
{
    /// <summary>
    /// What type of observable is this.
    /// </summary>
    public enum ObservableType
    {
        /// <summary>
        /// Traditional observable as defined my System.Reactive
        /// </summary>
        Traditional,

        /// <summary>
        /// As the source gets update, while the Observable is being consumed, new values will be returned when the Observable iteration reaches the values' indices. Upon hitting the end of the source, the observable is complete. If the source is cleared or the existing index is removed, the collection will complete.
        /// </summary>
        LiveUpdating,
        /// <summary>
        /// Same as <see cref="LiveUpdating"/> but will not complete at the end of the source. Any new values added after the current index of the observable will be returned.
        /// </summary>
        LiveUpdatingInfinite
    }

    /// <summary>
    /// Extensions for Reactive usage.
    /// </summary>
    public static class ReactiveExtensions
    {
        /// <summary>
        /// Convert a <see cref="ICollection{T}"/> into an <see cref="IObservable{T}"/> of a specific type.
        /// </summary>
        /// <typeparam name="T">Type of data.</typeparam>
        /// <param name="source">Source data.</param>
        /// <param name="type">What type of observable should be created.</param>
        /// <returns>An <see cref="IObservable{T}"/> based off the <paramref name="source"/>.</returns>
        public static IObservable<T> ToObservable<T>(this ICollection<T> source, ObservableType type)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            switch (type)
            {
                case ObservableType.LiveUpdating:
                case ObservableType.LiveUpdatingInfinite:
                    //TODO
                case ObservableType.Traditional:
                    return System.Reactive.Linq.Observable.ToObservable(source);
            }
            throw new ArgumentException("Unknown type", nameof(type));
        }

        /// <summary>
        /// Get the type of the <see cref="IObservable{T}"/>.
        /// </summary>
        /// <typeparam name="T"><Type of data./typeparam>
        /// <param name="source">Source observable.</param>
        /// <returns>The type of <see cref="IObservable{T}"/>.</returns>
        public static ObservableType GetObservableType<T>(this IObservable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            //TODO: if our custom type, then we can return the type
            return ObservableType.Traditional;
        }
    }
}
