using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;

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
        LiveUpdating
    }

    /// <summary>
    /// Extensions for Reactive usage.
    /// </summary>
    public static class ReactiveExtensions
    {
        /// <summary>
        /// Convert a <see cref="IList{T}"/> into an <see cref="IObservable{T}"/> of a specific type.
        /// </summary>
        /// <typeparam name="T">Type of data.</typeparam>
        /// <param name="source">Source data.</param>
        /// <param name="type">What type of observable should be created.</param>
        /// <returns>An <see cref="IObservable{T}"/> based off the <paramref name="source"/>.</returns>
        public static IObservable<T> ToObservable<T>(this IList<T> source, ObservableType type)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (type == ObservableType.Traditional)
            {
                // System.Reactive, at the time of this writing, uses CurrentThreadScheduler.Instance for the scheduler.
                // But in case that changes in the future, let the default ToObservable run instead of passing in the scheduler
                return System.Reactive.Linq.Observable.ToObservable(source);
            }

            return source.ToObservable(type, CurrentThreadScheduler.Instance);
        }

        /// <summary>
        /// Convert a <see cref="IList{T}"/> into an <see cref="IObservable{T}"/> of a specific type., using the specified scheduler to run the enumeration loop.
        /// </summary>
        /// <typeparam name="T">Type of data.</typeparam>
        /// <param name="source">Source data.</param>
        /// <param name="type">What type of observable should be created.</param>
        /// <param name="scheduler">Scheduler to run the enumeration of the input sequence on.</param>
        /// <returns>An <see cref="IObservable{T}"/> based off the <paramref name="source"/>.</returns>
        public static IObservable<T> ToObservable<T>(this IList<T> source, ObservableType type, IScheduler scheduler)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (scheduler == null)
            {
                throw new ArgumentNullException(nameof(scheduler));
            }

            switch (type)
            {
                case ObservableType.LiveUpdating:
                    return new LiveListObservable<T>(source, scheduler);
                case ObservableType.Traditional:
                    return System.Reactive.Linq.Observable.ToObservable(source, scheduler);
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
            if (source is TypedObservable<T> obs)
            {
                return obs.Type;
            }
            return ObservableType.Traditional;
        }
    }
}
