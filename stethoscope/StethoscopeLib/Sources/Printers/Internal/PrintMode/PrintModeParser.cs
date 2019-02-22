using Stateless;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Stethoscope.Printers.Internal.PrintMode
{
    /// <summary>
    /// Parser for print mode formats. Not to be confused with the printable type, <see cref="PrintModeFormat"/>.
    /// </summary>
    public class PrintModeParser : ICollection<IElement>
    {
        #region States and Triggers

        private enum State
        {
            Uninitialized,
            Setup,
            MethodCollection,
            FindProcessingRange,
            VisitExpressionTree,
            CountAndRemoveSkips,
            Done
        }

        private enum Trigger
        {
            Done,
            Invoke,

            HasMethods,
            NoMethods
        }

        #endregion

        private readonly List<IElement> elements = new List<IElement>();

        private PrintModeParser()
        {
        }
        
        /// <summary>
        /// Get any conditional that applies to the entire parsed string.
        /// </summary>
        public IConditional GlobalConditional => null; //TODO

        #region ICollection impl

        /// <summary>
        /// Gets the number of elements contained in the <see cref="PrintModeParser"/>.
        /// </summary>
        public int Count => elements.Count;

        /// <summary>
        /// <see cref="PrintModeParser"/> is not read-only, but can only be populated with <see cref="Parse(string, IPrinterElementFactory)"/>.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="item">Not supported</param>
        public void Add(IElement item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes all elements from the <see cref="PrintModeParser"/>.
        /// </summary>
        public void Clear()
        {
            elements.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="PrintModeParser"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="PrintModeParser"/>.</param>
        /// <returns><c>true</c> if item is found in the <see cref="PrintModeParser"/>; otherwise, <c>false</c>.</returns>
        public bool Contains(IElement item)
        {
            return elements.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="PrintModeParser"/> to an array starting at a particular array index.
        /// </summary>
        /// <param name="array">The destination for the elements.</param>
        /// <param name="arrayIndex">The index of <paramref name="array"/> to start copying elements into.</param>
        public void CopyTo(IElement[] array, int arrayIndex)
        {
            elements.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="item">Not supported</param>
        /// <returns>Not supported</returns>
        public bool Remove(IElement item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IElement> GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)elements).GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Parse a print mode format and add to self for storage.
        /// </summary>
        /// <param name="format">The print mode format to parse.</param>
        /// <param name="factory">The element factory to generate elements after parsing.</param>
        /// <returns>The parsed format.</returns>
        public static PrintModeParser Parse(string format, IPrinterElementFactory factory)
        {
            //TODO
            return new PrintModeParser(); //XXX
        }

        private StateMachine<State, Trigger> CreateStateMachine()
        {
            //TODO
            return null;
        }
    }
}
