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
        #region SMState

        private struct SMState : ICloneable
        {
            private IList<IElement> elements;

            public static SMState InvalidState = new SMState() { ErrorMessage = "Invalid Internal State" };

            public string Format { get; private set; }
            public IPrinterElementFactory ElementFactory { get; private set; }
            public int ParsingIndex { get; private set; }

            public string ErrorMessage { get; private set; }
            //TODO: log-level conditional

            public StateMachine<State, Trigger> StateMachine { get; private set; }
            public Action<SMState> DoneHandler { get; private set; }

            public SMState(string format, IPrinterElementFactory factory, IList<IElement> elements, StateMachine<State, Trigger> stateMachine, Action<SMState> doneHandler)
            {
                Format = format;
                ElementFactory = factory;
                this.elements = elements;
                ParsingIndex = 0;

                ErrorMessage = null;

                StateMachine = stateMachine;
                DoneHandler = doneHandler;
            }

            public SMState IncrementIndex(int charCount)
            {
                var clone = (SMState)Clone();
                clone.ParsingIndex += charCount;
                return clone;
            }

            public SMState SetError(string errorMessage)
            {
                var clone = (SMState)Clone();
                clone.ErrorMessage = errorMessage;
                return clone;
            }

            public void AddElement(IElement element) //XXX don't like making this a mutible variable... other option is to store everything in SMState and then set the external elements list
            {
                elements.Add(element);
            }

            public object Clone()
            {
                return new SMState()
                {
                    Format = Format,
                    ElementFactory = ElementFactory,
                    elements = elements,
                    ParsingIndex = ParsingIndex,

                    ErrorMessage = ErrorMessage,

                    StateMachine = StateMachine,
                    DoneHandler = DoneHandler
                };
            }
        }

        #endregion

        #region States and Triggers

        private enum State
        {
            Uninitialized,
            Setup,
            Done,

            // Main sections
            LogConditional,
            Raw,
            Part,

            // Part sections
            Attribute,
            Conditional,
            Modifier,
            AttributeReference,
            AttributeFormat,

            // Utility
            StringQuote,

            // Conditionals
            ErrorHandlerConditional
        }

        private enum Trigger
        {
            Done,
            Invoke,
            Error,

            FoundConditional,
            FoundFormat,
            FoundPart,
            FoundQuote
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
            var parser = new PrintModeParser();
            parser.InternalProcess(format, factory);
            return parser;
        }

        private void InternalProcess(string format, IPrinterElementFactory factory)
        {
            var finishedState = SMState.InvalidState;
            void ProcessIsComplete(SMState state)
            {
                finishedState = state;
            }

            var stateMachine = CreateStateMachine();
            stateMachine.Fire(processTrigger, (format, factory, elements, ProcessIsComplete));

            if (finishedState.ErrorMessage != null)
            {
                throw new ArgumentException(finishedState.ErrorMessage);
            }
            //TODO: get/update variables based on state
        }

        #region Create State Machine

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<(string, IPrinterElementFactory, IList<IElement>, Action<SMState>)> processTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<(string, IPrinterElementFactory, IList<IElement>, Action<SMState>)>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> invokeTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> doneTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Done);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> errorTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Error);

        private StateMachine<State, Trigger> CreateStateMachine()
        {
            var machine = new StateMachine<State, Trigger>(State.Uninitialized);

            machine.Configure(State.Uninitialized)
                .Permit(Trigger.Invoke, State.Setup);

            machine.Configure(State.Setup)
                .OnEntryFrom(processTrigger, ((string format, IPrinterElementFactory factory, IList<IElement> elements, Action<SMState> doneHandler) processParameters) =>
                {
                    var internalState = new SMState(processParameters.format, processParameters.factory, processParameters.elements, machine, processParameters.doneHandler);
                    if (internalState.Format.Length > 0 && internalState.Format[0] == '@')
                    {
                        internalState = internalState.IncrementIndex(1);
                    }
                    machine.Fire(doneTrigger, internalState);
                })
                .PermitReentry(Trigger.Invoke)
                .Permit(Trigger.Done, State.LogConditional);

            machine.Configure(State.LogConditional)
                .OnEntryFrom(doneTrigger, (state, transition) =>
                {
                    //TODO: check for format to determine if it's a conditional or a part
                })
                .PermitReentry(Trigger.FoundConditional)
                .Permit(Trigger.Done, State.Part)
                .Permit(Trigger.Error, State.Done);

            //TODO

            machine.Configure(State.Done)
                .OnEntryFrom(doneTrigger, state => state.DoneHandler(state))
                .OnEntryFrom(errorTrigger, state => state.DoneHandler(state))
                .PermitIf(processTrigger, State.Setup);

            return machine;
        }

        #endregion
    }
}
