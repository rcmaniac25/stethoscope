using Stateless;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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
            public static SMState InvalidState = new SMState() { ErrorMessage = "Invalid Internal State" };

            public string Format { get; private set; }
            public IPrinterElementFactory ElementFactory { get; private set; }
            public int ParsingIndex { get; private set; }

            public string ErrorMessage { get; private set; }

            public List<IElement> Elements { get; private set; }
            public List<IConditional> LogConditionals { get; private set; }

            public StateMachine<State, Trigger> StateMachine { get; private set; }
            public Action<SMState> DoneHandler { get; private set; }

            public SMState(string format, IPrinterElementFactory factory, StateMachine<State, Trigger> stateMachine, Action<SMState> doneHandler)
            {
                Format = format;
                ElementFactory = factory;
                ParsingIndex = 0;

                ErrorMessage = null;

                Elements = null;
                LogConditionals = null;

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

            public SMState AddElement(IElement element)
            {
                var clone = (SMState)Clone();
                if (clone.Elements == null)
                {
                    clone.Elements = new List<IElement>();
                }
                clone.Elements.Add(element);
                return clone;
            }

            public SMState AddLogConditional(IConditional conditional)
            {
                var clone = (SMState)Clone();
                if (clone.LogConditionals == null)
                {
                    clone.LogConditionals = new List<IConditional>();
                }
                clone.LogConditionals.Add(conditional);
                return clone;
            }

            public bool TestCharLength(int charCount)
            {
                return (ParsingIndex + charCount) <= Format.Length;
            }

            public object Clone()
            {
                var cloneElements = Elements != null ? new List<IElement>(Elements) : null;
                var cloneLogConditionals = Elements != null ? new List<IConditional>(LogConditionals) : null;

                return new SMState()
                {
                    Format = Format,
                    ElementFactory = ElementFactory,
                    ParsingIndex = ParsingIndex,

                    ErrorMessage = ErrorMessage,

                    Elements = cloneElements,
                    LogConditionals = cloneLogConditionals,

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
            CountTillMarker,
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
            FoundQuote,

            ProcessRaw,
            ProcessAttribute
        }

        #endregion

        private List<IElement> elements;

        private PrintModeParser()
        {
        }

        /// <summary>
        /// Get any conditional that applies to the entire parsed string.
        /// </summary>
        public IConditional GlobalConditional { get; private set; }

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
            stateMachine.Fire(processTrigger, format, factory, ProcessIsComplete);

            if (finishedState.ErrorMessage != null)
            {
                throw new ArgumentException(finishedState.ErrorMessage);
            }
            elements = finishedState.Elements;
            if (finishedState.LogConditionals?.Count > 0)
            {
                GlobalConditional = finishedState.LogConditionals[0];
            }
        }

        #region Create State Machine

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string, IPrinterElementFactory, Action<SMState>> processTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<string, IPrinterElementFactory, Action<SMState>>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> invokeTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState, char[]> countTillMarkerTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState, char[]>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> doneTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Done);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState, int> doneIntTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState, int>(Trigger.Done);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> errorTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Error);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> processRawTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.ProcessRaw);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> foundConditionalTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.FoundConditional);

        private StateMachine<State, Trigger> CreateStateMachine()
        {
            var machine = new StateMachine<State, Trigger>(State.Uninitialized);

            machine.Configure(State.Uninitialized)
                .Permit(Trigger.Invoke, State.Setup);

            machine.Configure(State.Setup)
                .OnEntryFrom(processTrigger, (string format, IPrinterElementFactory factory, Action<SMState> doneHandler) =>
                {
                    var internalState = new SMState(format, factory, machine, doneHandler);
                    if (internalState.Format.Length > 0 && internalState.Format[0] == '@')
                    {
                        internalState = internalState.IncrementIndex(1);
                    }
                    machine.Fire(doneTrigger, internalState);
                })
                .PermitReentry(Trigger.Invoke)
                .Permit(Trigger.Done, State.LogConditional);

            machine.Configure(State.LogConditional)
                .OnEntryFrom(doneTrigger, HandleLogConditional)
                .PermitReentry(Trigger.FoundConditional)
                .Permit(Trigger.Done, State.Part)
                .Permit(Trigger.Error, State.Done);

            machine.Configure(State.Part)
                .OnEntryFrom(doneTrigger, state => state.StateMachine.Fire(countTillMarkerTrigger, state, PartAttributeIdentifiers))
                .OnEntryFrom(doneIntTrigger, HandlePart)
                .Permit(Trigger.Invoke, State.CountTillMarker)
                .Permit(Trigger.ProcessRaw, State.Raw)
                .Permit(Trigger.ProcessAttribute, State.Attribute)
                .Permit(Trigger.Done, State.Done)
                .Permit(Trigger.Error, State.Done);

            machine.Configure(State.CountTillMarker)
                .OnEntryFrom(countTillMarkerTrigger, CountTillMarker)
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

        #region Parser Components

        private static readonly Dictionary<char, ConditionalElement> LogConditionalFormatElements = new Dictionary<char, ConditionalElement>()
        {
            { '+', ConditionalElement.ValidLog },
            { '-', ConditionalElement.InvalidLog }
        };

        private static readonly Dictionary<char, ConditionalElement> AttributeConditionalFormatElements = new Dictionary<char, ConditionalElement>(LogConditionalFormatElements)
        {
            { '^', ConditionalElement.AttributeExists },
            { '$', ConditionalElement.AttributeValueChanged },
            { '~', ConditionalElement.AttributeValueNew }
        };

        private static readonly Dictionary<char, ModifierElement> AttributeModiferFormatElements = new Dictionary<char, ModifierElement>()
        {
            { '!', ModifierElement.ErrorHandler }
        };

        private static readonly char[] PartAttributeIdentifiers = AttributeConditionalFormatElements.Select(x => x.Key).Concat(new char[] { '{' }).ToArray();

        #endregion

        #region State Entry Operations

        private void HandleLogConditional(SMState state)
        {
            if (state.TestCharLength(1))
            {
                var type = state.Format[state.ParsingIndex];

                // Need to test that we're not looking at a raw that is using the special chars (+ means conditional, ++ means it's a raw char of '+')
                // As such, only passes if an odd number of chars match. + = conditional, ++ = raw, +++ = conditional + raw, ++++ = 2x raw, etc.
                var count = 1;
                while (state.TestCharLength(count + 1) && state.Format[state.ParsingIndex + count] == type)
                {
                    count++;
                }
                if (count % 2 == 1 && LogConditionalFormatElements.ContainsKey(type))
                {
                    if (state.LogConditionals?.Count != 0)
                    {
                        state.StateMachine.Fire(errorTrigger, state.SetError("Only one log-level conditional is allowed"));
                        return;
                    }
                    var conditional = state.ElementFactory.CreateConditional(LogConditionalFormatElements[type]);
                    state.StateMachine.Fire(foundConditionalTrigger, state.AddLogConditional(conditional).IncrementIndex(1));
                    return;
                }
            }
            state.StateMachine.Fire(doneTrigger, state);
        }

        private void CountTillMarker(SMState state, char[] termChars)
        {
            //TODO
        }

        private void HandlePart(SMState state, int partLength)
        {
            /* 1. Search for the start of a new part/end of string
             * 2. If distance == 0, goto step 4
             * 3. ProcessRaw (length of raw section)
             * 4. If index == end of string, Done
             * 5. ProcessAttribute
             */

            // ProcessRaw, ProcessAttribute, Done, Error
            //TODO
        }

        #endregion
    }
}
