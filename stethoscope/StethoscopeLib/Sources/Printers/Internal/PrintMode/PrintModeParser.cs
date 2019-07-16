using Stateless;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using Stethoscope.Common;

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
            public static LogAttribute InvalidLogAttribute = LogAttribute.Timestamp - 1;

            public string Format { get; private set; }
            public IPrinterElementFactory ElementFactory { get; private set; }
            public int ParsingIndex { get; private set; }

            public string ErrorMessage { get; private set; }

            public List<IConditional> CurrentElementConditionals { get; private set; }
            public LogAttribute CurrentLogAttribute { get; private set; }
            public string CurrentLogFormat { get; private set; }
            public List<IModifier> CurrentElementModifiers { get; private set; }

            public List<IElement> Elements { get; private set; }
            public List<IConditional> LogConditionals { get; private set; }
            public List<IModifier> LogModifiers { get; private set; }

            public State PriorState { get; private set; }
            public StateMachine<State, Trigger> StateMachine { get; private set; }
            public Action<SMState> DoneHandler { get; private set; }

            public SMState(string format, IPrinterElementFactory factory, StateMachine<State, Trigger> stateMachine, Action<SMState> doneHandler)
            {
                Format = format;
                ElementFactory = factory;
                ParsingIndex = 0;

                ErrorMessage = null;

                CurrentElementConditionals = null;
                CurrentLogAttribute = InvalidLogAttribute;
                CurrentLogFormat = null;
                CurrentElementModifiers = null;

                Elements = null;
                LogConditionals = null;
                LogModifiers = null;

                PriorState = State.Uninitialized;
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
                clone.ErrorMessage = $"ERROR: Index {ParsingIndex}. Message: {errorMessage}";
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

            public SMState AddLogModifier(IModifier modifier)
            {
                var clone = (SMState)Clone();
                if (clone.LogModifiers == null)
                {
                    clone.LogModifiers = new List<IModifier>();
                }
                clone.LogModifiers.Add(modifier);
                return clone;
            }

            public SMState AddConditionalForCurrentElement(IConditional conditional)
            {
                var clone = (SMState)Clone();
                if (clone.CurrentElementConditionals == null)
                {
                    clone.CurrentElementConditionals = new List<IConditional>();
                }
                clone.CurrentElementConditionals.Add(conditional);
                return clone;
            }

            public SMState SetAttributeForCurrentElement(LogAttribute attribute)
            {
                var clone = (SMState)Clone();
                clone.CurrentLogAttribute = attribute;
                return clone;
            }

            public SMState SetFormatForCurrentElement(string format)
            {
                var clone = (SMState)Clone();
                clone.CurrentLogFormat = format;
                return clone;
            }

            public SMState AddModifierForCurrentElement(IModifier modifier)
            {
                var clone = (SMState)Clone();
                if (clone.CurrentElementModifiers == null)
                {
                    clone.CurrentElementModifiers = new List<IModifier>();
                }
                clone.CurrentElementModifiers.Add(modifier);
                return clone;
            }

            public SMState ResetCurrentElementValues()
            {
                var clone = (SMState)Clone();
                clone.CurrentElementConditionals = null;
                clone.CurrentLogAttribute = InvalidLogAttribute;
                clone.CurrentLogFormat = null;
                clone.CurrentElementModifiers = null;
                return clone;
            }

            public SMState SetPriorState(State state)
            {
                var clone = (SMState)Clone();
                clone.PriorState = state;
                return clone;
            }

            public bool TestCharLength(int charCount)
            {
                return (ParsingIndex + charCount) <= Format.Length;
            }

            public string FormatSubstring(int length)
            {
                return FormatSubstring(0, length);
            }

            public string FormatSubstring(int startIndex, int length)
            {
                return Format.Substring(ParsingIndex + startIndex, length);
            }

            public char CurrentFormatChar
            {
                get
                {
                    return Format[ParsingIndex];
                }
            }

            public object Clone()
            {
                var cloneCurrentElementConditionals = CurrentElementConditionals != null ? new List<IConditional>(CurrentElementConditionals) : null;
                var cloneCurrentElementModifiers = CurrentElementModifiers != null ? new List<IModifier>(CurrentElementModifiers) : null;
                var cloneElements = Elements != null ? new List<IElement>(Elements) : null;
                var cloneLogConditionals = LogConditionals != null ? new List<IConditional>(LogConditionals) : null;
                var cloneLogModifiers = LogModifiers != null ? new List<IModifier>(LogModifiers) : null;

                return new SMState()
                {
                    Format = Format,
                    ElementFactory = ElementFactory,
                    ParsingIndex = ParsingIndex,

                    ErrorMessage = ErrorMessage,

                    CurrentElementConditionals = cloneCurrentElementConditionals,
                    CurrentLogAttribute = CurrentLogAttribute,
                    CurrentLogFormat = CurrentLogFormat,
                    CurrentElementModifiers = cloneCurrentElementModifiers,

                    Elements = cloneElements,
                    LogConditionals = cloneLogConditionals,
                    LogModifiers = cloneLogModifiers,

                    PriorState = PriorState,
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
            Error,

            // Main sections
            LogConditional,
            LogModifier,
            Raw,
            Part,

            // Part sections
            Attribute,
            Conditional,
            Modifier,
            AttributeReference,
            AttributeFormat,
            FinalizePart,

            // Utility
            CountTillMarker,
            StringQuote,

            // Modifiers
            ErrorHandlerModifier
        }

        private enum Trigger
        {
            Done,
            Invoke,
            Error,

            FoundConditional,
            FoundModifier,
            FoundFormat,
            FoundPart,
            FoundQuote,

            ProcessRaw,
            ProcessAttribute
        }

        #endregion

        #region Special Chars

        [Flags]
        private enum SpecialCharFlags
        {
            None = 0,

            // Location for attribute
            StartAttribute = 1 << 0,
            EndAttribute = 1 << 1,

            // Type
            Conditional = 1 << 2,
            Modifier = 1 << 3,
            Attribute = 1 << 4,

            // Where it can be used
            ForLog = 1 << 5,
            ForAttribute = 1 << 6
        }

        private struct SpecialCharValues
        {
            public SpecialCharFlags Flags { get; set; }
            public ConditionalElement ConditionalElement { get; set; }
            public ModifierElement ModifierElement { get; set; }

            public static SpecialCharValues Create(SpecialCharFlags flags)
            {
                return new SpecialCharValues
                {
                    Flags = flags
                };
            }

            public static SpecialCharValues Create(SpecialCharFlags flags, ConditionalElement conditional)
            {
                return new SpecialCharValues
                {
                    Flags = flags,
                    ConditionalElement = conditional
                };
            }

            public static SpecialCharValues Create(SpecialCharFlags flags, ModifierElement modifier)
            {
                return new SpecialCharValues
                {
                    Flags = flags,
                    ModifierElement = modifier
                };
            }
        }

        private const char SPECIAL_CHAR_MOD_ERROR_HANDLER = '!';
        private const char SPECIAL_CHAR_ATT_FORMAT = '|';
        private const char SPECIAL_CHAR_ATT_START = '{';
        private const char SPECIAL_CHAR_ATT_END = '}';

        private static readonly Dictionary<char, SpecialCharValues> SpecialChars = new Dictionary<char, SpecialCharValues>()
        {
            { 'v', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForLog, ConditionalElement.ValidLog) },
            { 'i', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForLog, ConditionalElement.InvalidLog) },

            { '+', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForAttribute | SpecialCharFlags.StartAttribute, ConditionalElement.ValidLog) },
            { '-', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForAttribute | SpecialCharFlags.StartAttribute, ConditionalElement.InvalidLog) },
            { '^', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForAttribute | SpecialCharFlags.StartAttribute, ConditionalElement.AttributeExists) },
            { '$', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForAttribute | SpecialCharFlags.StartAttribute, ConditionalElement.AttributeValueChanged) },
            { '~', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForAttribute | SpecialCharFlags.StartAttribute, ConditionalElement.AttributeValueNew) },

            { SPECIAL_CHAR_MOD_ERROR_HANDLER, SpecialCharValues.Create(SpecialCharFlags.Modifier | SpecialCharFlags.ForLog | SpecialCharFlags.ForAttribute | SpecialCharFlags.EndAttribute, ModifierElement.ErrorHandler) },

            { SPECIAL_CHAR_ATT_START, SpecialCharValues.Create(SpecialCharFlags.Attribute | SpecialCharFlags.StartAttribute) },
            { SPECIAL_CHAR_ATT_END, SpecialCharValues.Create(SpecialCharFlags.Attribute | SpecialCharFlags.EndAttribute) }
        };

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
#if DEBUG
            //XXX temp while developing
            string s = Stateless.Graph.UmlDotGraph.Format(stateMachine.GetInfo());
#endif
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

        //XXX replace GetSpecialCharsForFlags with this function eventually (if possible)
        private static IEnumerable<char> GetSpecialCharsForFlagsEn(SpecialCharFlags flags)
        {
            foreach (var schar in SpecialChars)
            {
                if ((schar.Value.Flags & flags) == flags)
                {
                    yield return schar.Key;
                }
            }
        }

        private static char[] GetSpecialCharsForFlags(SpecialCharFlags flags)
        {
            return GetSpecialCharsForFlagsEn(flags).ToArray();
        }

        #region Create State Machine

        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string, IPrinterElementFactory, Action<SMState>> processTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<string, IPrinterElementFactory, Action<SMState>>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> invokeTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState, char[], CountTillFlag> countTillMarkerTriggerWithFlags = new StateMachine<State, Trigger>.TriggerWithParameters<SMState, char[], CountTillFlag>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState, char[]> countTillMarkerTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState, char[]>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> doneTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Done);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState, int> doneIntTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState, int>(Trigger.Done);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> errorTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Error);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState, int> processRawTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState, int>(Trigger.ProcessRaw);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> processAttributeTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.ProcessAttribute);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> foundConditionalTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.FoundConditional);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> foundModifierTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.FoundModifier);

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
                .Permit(Trigger.Done, State.LogConditional);
            
            machine.Configure(State.LogConditional)
                .OnEntryFrom(doneTrigger, HandleLogConditional)
                .OnEntryFrom(foundConditionalTrigger, HandleLogConditional)
                .PermitReentry(Trigger.FoundConditional)
                .Permit(Trigger.Done, State.LogModifier)
                .Permit(Trigger.Invoke, State.Part)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.LogModifier)
                .OnEntryFrom(doneTrigger, HandleLogModifier)
                .OnEntryFrom(foundModifierTrigger, HandleLogModifier)
                .PermitReentry(Trigger.FoundModifier)
                .Permit(Trigger.Done, State.Part)
                .Permit(Trigger.Invoke, State.ErrorHandlerModifier)
                .Permit(Trigger.Error, State.Error);
            
            machine.Configure(State.Part)
                .OnEntryFrom(doneTrigger, state => state.StateMachine.Fire(countTillMarkerTriggerWithFlags, state.SetPriorState(State.Part), GetSpecialCharsForFlags(SpecialCharFlags.StartAttribute), CountTillFlag.ObserveSpecialChars | CountTillFlag.LastMarker))
                .OnEntryFrom(doneIntTrigger, HandlePart)
                .Permit(Trigger.Invoke, State.CountTillMarker)
                .Permit(Trigger.ProcessRaw, State.Raw)
                .Permit(Trigger.ProcessAttribute, State.Attribute)
                .Permit(Trigger.Done, State.Done)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.Raw)
                .OnEntryFrom(processRawTrigger, ProcessRaw)
                .Permit(Trigger.Done, State.Part)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.Attribute)
                .OnEntryFrom(processAttributeTrigger, HandleAttribute)
                .Permit(Trigger.FoundConditional, State.Conditional)
                .Permit(Trigger.Done, State.AttributeReference)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.Conditional)
                .OnEntryFrom(foundConditionalTrigger, HandleConditional)
                .PermitReentry(Trigger.FoundConditional)
                .Permit(Trigger.Done, State.AttributeReference)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.AttributeReference)
                .OnEntryFrom(doneTrigger, state => state.StateMachine.Fire(countTillMarkerTriggerWithFlags, state.SetPriorState(State.AttributeReference), GetSpecialCharsForFlagsEn(SpecialCharFlags.EndAttribute).Concat(new char[] { SPECIAL_CHAR_ATT_FORMAT }).ToArray(), CountTillFlag.IgnoreSpecialChars | CountTillFlag.FirstMarker))
                .OnEntryFrom(doneIntTrigger, HandleAttributeReference)
                .Permit(Trigger.Invoke, State.CountTillMarker)
                .Permit(Trigger.Done, State.Modifier)
                .Permit(Trigger.Invoke, State.AttributeFormat)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.AttributeFormat)
                .OnEntryFrom(invokeTrigger, state => state.StateMachine.Fire(countTillMarkerTriggerWithFlags, state.SetPriorState(State.AttributeFormat), GetSpecialCharsForFlags(SpecialCharFlags.EndAttribute), CountTillFlag.ObserveSpecialChars | CountTillFlag.LastMarker))
                .OnEntryFrom(doneIntTrigger, HandleAttributeFormat)
                .Permit(Trigger.Invoke, State.CountTillMarker)
                .Permit(Trigger.Done, State.Modifier)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.Modifier)
                .OnEntryFrom(doneTrigger, HandleModifer)
                .OnEntryFrom(foundModifierTrigger, HandleModifer)
                .PermitReentry(Trigger.FoundModifier)
                .Permit(Trigger.Invoke, State.ErrorHandlerModifier)
                .Permit(Trigger.Done, State.FinalizePart)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.FinalizePart)
                .OnEntryFrom(doneTrigger, HandleFinalizePart)
                .Permit(Trigger.Done, State.Part)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.Done)
                .OnEntryFrom(doneTrigger, state => state.DoneHandler(state))
                .PermitIf(processTrigger, State.Setup);

            machine.Configure(State.Error)
                .OnEntryFrom(errorTrigger, state =>
                {
                    if (string.IsNullOrWhiteSpace(state.ErrorMessage))
                    {
                        state.DoneHandler(state.SetError("Unknown error occured"));
                    }
                    else
                    {
                        state.DoneHandler(state);
                    }
                }).PermitIf(processTrigger, State.Setup);

            machine.Configure(State.CountTillMarker)
                .OnEntryFrom(countTillMarkerTriggerWithFlags, CountTillMarker)
                .OnEntryFrom(countTillMarkerTrigger, (SMState state, char[] termChars) => state.StateMachine.Fire(countTillMarkerTriggerWithFlags, state, termChars, CountTillFlag.ObserveSpecialChars | CountTillFlag.FirstMarker))
                .PermitReentry(Trigger.Invoke)
                .PermitDynamic(doneTrigger, state => state.PriorState);

            machine.Configure(State.ErrorHandlerModifier)
                .OnEntryFrom(invokeTrigger, HandleErrorModifierSanityCheck)
                .OnEntryFrom(doneTrigger, HandleErrorModifier)
                .Permit(Trigger.Invoke, State.StringQuote)
                .Permit(Trigger.Error, State.Error)
                .PermitDynamic(doneTrigger, state => state.PriorState);

            return machine;
        }

        #endregion
        
        #region State Entry Operations

        private void HandleLogConditional(SMState state)
        {
            // See #350.2 and #434.2 for overview of what's happening

            if (state.TestCharLength(1))
            {
                var type = state.CurrentFormatChar;
                
                // Need to test that we're not looking at a raw that is using the special chars (+ means conditional, ++ means it's a raw char of '+')
                // As such, only passes if an odd number of chars match. + = conditional, ++ = raw, +++ = conditional + raw, ++++ = 2x raw, etc.
                var count = 1;
                while (state.TestCharLength(count + 1) && state.Format[state.ParsingIndex + count] == type)
                {
                    count++;
                }
                if (GetSpecialCharsForFlagsEn(SpecialCharFlags.Conditional | SpecialCharFlags.ForLog).Contains(type))
                {
                    var conditionalElement = SpecialChars[type].ConditionalElement;
                    
                    if (count % 2 == 0)
                    {
                        // Escaped chars can be processed by the Part state, and since it's a doubling of chars, having it parse half them
                        state.StateMachine.Fire(invokeTrigger, state.IncrementIndex(count / 2));
                    }
                    else
                    {
                        var conditional = state.ElementFactory.CreateConditional(conditionalElement);
                        if (conditional == null)
                        {
                            state.StateMachine.Fire(errorTrigger, state.SetError($"Could not create log conditional: {type}"));
                        }
                        else if (count == 1)
                        {
                            // Only one conditional was found
                            state.StateMachine.Fire(foundConditionalTrigger, state.AddLogConditional(conditional).IncrementIndex(1));
                        }
                        else
                        {
                            // A conditional was found, but so was escape chars... so we need to escape the right amount (letting the Part state do the work)
                            int inc = 1 + ((count - 1) / 2);
                            state.StateMachine.Fire(invokeTrigger, state.AddLogConditional(conditional).IncrementIndex(inc));
                        }
                    }
                    return;
                }
            }
            state.StateMachine.Fire(doneTrigger, state);
        }

        private void HandleLogModifier(SMState state)
        {
            // See #350.2 for overview of what's happening

            if (state.TestCharLength(1))
            {
                var type = state.CurrentFormatChar;

                // Need to test that we're not looking at a raw that is using the special chars (+ means conditional, ++ means it's a raw char of '+')
                // As such, only passes if an odd number of chars match. + = conditional, ++ = raw, +++ = conditional + raw, ++++ = 2x raw, etc.
                var count = 1;
                while (state.TestCharLength(count + 1) && state.Format[state.ParsingIndex + count] == type)
                {
                    count++;
                }
                if (count % 2 == 1 && GetSpecialCharsForFlagsEn(SpecialCharFlags.Modifier | SpecialCharFlags.ForLog).Contains(type))
                {
                    var modifierElement = SpecialChars[type].ModifierElement;

                    if (state.LogModifiers?.Where(c => c.Type == modifierElement).Count() > 0)
                    {
                        if (count == 1)
                        {
                            state.StateMachine.Fire(errorTrigger, state.SetError($"Only one modifier is allowed of each type. This type: {type}"));
                        }
                    }
                    else
                    {
                        if (modifierElement == ModifierElement.ErrorHandler)
                        {
                            state.StateMachine.Fire(invokeTrigger, state.SetPriorState(State.LogModifier));
                            return;
                        }
                        var modifier = state.ElementFactory.CreateModifier(modifierElement);
                        if (modifier == null)
                        {
                            state.StateMachine.Fire(errorTrigger, state.SetError($"Could not create log modifier: {type}"));
                        }
                        else
                        {
                            state.StateMachine.Fire(foundModifierTrigger, state.AddLogModifier(modifier).IncrementIndex(1));
                        }
                        return;
                    }
                }
            }
            state.StateMachine.Fire(doneTrigger, state);
        }

        [Flags]
        private enum CountTillFlag
        {
            /// <summary>
            /// If a char is a special char, treat it as something that may be raw char (repeating itself to excape itself) or if it is a terminator marker (not repeated).
            /// </summary>
            ObserveSpecialChars = 0x00,
            /// <summary>
            /// If a char is a special char, ignore that it's a special char and treat it a single, non-repeated char.
            /// </summary>
            IgnoreSpecialChars = 0x01,

            /// <summary>
            /// Mask to get special char flags
            /// </summary>
            SpecialCharMask = 0x01,
            /// <summary>
            /// Bit shift for special char flags
            /// </summary>
            SpecialCharOffset = 0,

            /// <summary>
            /// Stop at the first marker. So ^ (raw = ^^) of "abc^^^" would return 3
            /// </summary>
            FirstMarker = 0x00,
            /// <summary>
            /// Stop at the last marker. So ^ (raw = ^^) of "abc^^^" would return 5
            /// </summary>
            LastMarker = 0x02,

            /// <summary>
            /// Mask to get marker location
            /// </summary>
            MarkerLocationMask = 0x02,
            /// <summary>
            /// Not shift for marker location flags
            /// </summary>
            MarkerLocationOffset = 1
        }

        private static int? CountTillMarkerCalculateLength(SMState state, ref int testLen, char c, CountTillFlag flag)
        {
            var notRaw = true;
            if ((flag & CountTillFlag.SpecialCharMask) == CountTillFlag.ObserveSpecialChars && SpecialChars.ContainsKey(c))
            {
                var innerCount = 1;
                while (state.TestCharLength(innerCount + testLen) && state.Format[state.ParsingIndex + innerCount + testLen - 1] == c)
                {
                    innerCount++;
                }
                var testLastMarker = innerCount > 1 && (flag & CountTillFlag.MarkerLocationMask) == CountTillFlag.LastMarker;
                if (innerCount % 2 == 0 || testLastMarker)
                {
                    notRaw = false;
                    testLen += innerCount - 1;

                    if (innerCount % 2 != 0 && testLastMarker)
                    {
                        // In the case we want the last marker, we don't want the outer loop to skip over it, so we subtract 1 from the test length, so the next call will hit it
                        testLen--;
                    }
                }
            }

            if (notRaw)
            {
                return testLen - 1;
            }
            return null;
        }

        private void CountTillMarker(SMState state, char[] termChars, CountTillFlag flag)
        {
            var len = 0;
            var testLen = 1;
            while (state.TestCharLength(testLen))
            {
                var c = state.Format[state.ParsingIndex + testLen - 1];
                if (termChars.Contains(c))
                {
                    var finalLen = CountTillMarkerCalculateLength(state, ref testLen, c, flag);
                    if (finalLen.HasValue)
                    {
                        len = finalLen.Value;
                        break;
                    }
                }
                testLen++;
            }
            state.StateMachine.Fire(doneIntTrigger, state, len);
        }

        private void HandlePart(SMState state, int partLength)
        {
            /* 1. Search for the start of a new part/end of string
             * 2. If distance == 0, goto step 4
             * 3. ProcessRaw (length of raw section)
             * 4. If index == end of string, Done
             * 5. ProcessAttribute
             */

            if (partLength <= 0)
            {
                if (partLength < 0)
                {
                    state.StateMachine.Fire(errorTrigger, state.SetError($"Part length cannot be less then 0. Was {partLength}"));
                }
                else if (state.ParsingIndex == state.Format.Length)
                {
                    state.StateMachine.Fire(doneTrigger, state);
                }
                else
                {
                    state.StateMachine.Fire(processAttributeTrigger, state);
                }
            }
            else
            {
                state.StateMachine.Fire(processRawTrigger, state, partLength);
            }
        }

        private static string CollapseSpecialCharsInRaw(string str)
        {
            if (!str.Any(SpecialChars.ContainsKey))
            {
                return str;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (SpecialChars.ContainsKey(c))
                {
                    // Skip chars
                    var k = i + 1;
                    while (k < str.Length && str[k] == c)
                    {
                        k++;
                    }
                    i = k - 1;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        private void ProcessRaw(SMState state, int partLength)
        {
            if (partLength <= 0)
            {
                if (partLength == 0)
                {
                    // Not sure why this would happened
                    //XXX add stat counter for this
                    state.StateMachine.Fire(doneTrigger, state);
                }
                else
                {
                    state.StateMachine.Fire(errorTrigger, state.SetError($"Raw element length cannot be less then 0. Was {partLength}"));
                }
            }
            else
            {
                var str = CollapseSpecialCharsInRaw(state.FormatSubstring(partLength));

                var rawElement = state.ElementFactory.CreateRaw(str);
                if (rawElement == null)
                {
                    state.StateMachine.Fire(errorTrigger, state.SetError($"Could not create raw: {str}"));
                }
                else
                {
                    state.StateMachine.Fire(doneTrigger, state.AddElement(rawElement).IncrementIndex(partLength));
                }
            }
        }

        private void HandleAttribute(SMState state)
        {
            if (state.TestCharLength(1))
            {
                var c = state.CurrentFormatChar;

                if (GetSpecialCharsForFlagsEn(SpecialCharFlags.Conditional | SpecialCharFlags.ForAttribute).Contains(c))
                {
                    state.StateMachine.Fire(foundConditionalTrigger, state);
                }
                else if (GetSpecialCharsForFlagsEn(SpecialCharFlags.Attribute | SpecialCharFlags.StartAttribute).Contains(c))
                {
                    state.StateMachine.Fire(doneTrigger, state);
                }
                else
                {
                    state.StateMachine.Fire(errorTrigger, state.SetError($"Unknown marker: {c}"));
                }
            }
            else
            {
                state.StateMachine.Fire(errorTrigger, state.SetError("End of format reached unexpectedly"));
            }
        }

        private void HandleConditional(SMState state)
        {
            //XXX need to support error handler conditional
            if (state.TestCharLength(1))
            {
                var type = state.CurrentFormatChar;

                if (GetSpecialCharsForFlagsEn(SpecialCharFlags.Conditional | SpecialCharFlags.ForAttribute).Contains(type))
                {
                    var conditional = state.ElementFactory.CreateConditional(SpecialChars[type].ConditionalElement);
                    if (conditional == null)
                    {
                        state.StateMachine.Fire(errorTrigger, state.SetError($"Could not create conditional: {type}"));
                    }
                    else
                    {
                        state.StateMachine.Fire(foundConditionalTrigger, state.AddConditionalForCurrentElement(conditional).IncrementIndex(1));
                    }
                    return;
                }
            }
            state.StateMachine.Fire(doneTrigger, state);
        }

        private void HandleAttributeReference(SMState state, int partLength)
        {
            if (partLength <= 0)
            {
                state.StateMachine.Fire(errorTrigger, state.SetError("Attribute reference is empty and doesn't exist"));
                return;
            }
            else if (state.CurrentFormatChar != SPECIAL_CHAR_ATT_START)
            {
                state.StateMachine.Fire(errorTrigger, state.SetError($"Attribute reference has no starting '{SPECIAL_CHAR_ATT_START}'"));
                return;
            }

            var attRefStr = state.FormatSubstring(1, partLength - 1);
            if (!Enum.TryParse(attRefStr, out LogAttribute attRef))
            {
                state.StateMachine.Fire(errorTrigger, state.SetError($"Unknown attribute {attRefStr}"));
                return;
            }

            var newState = state.SetAttributeForCurrentElement(attRef).IncrementIndex(partLength);
            if (newState.TestCharLength(1))
            {
                // Invoke = Attribute Format, Done = Done with reference
                newState.StateMachine.Fire(newState.CurrentFormatChar == SPECIAL_CHAR_ATT_FORMAT ? invokeTrigger : doneTrigger, newState.IncrementIndex(1));
            }
            else
            {
                newState.StateMachine.Fire(errorTrigger, newState.SetError($"Unexpected end of attribute. Expected '{SPECIAL_CHAR_ATT_END}'"));
            }
        }
        
        private void HandleAttributeFormat(SMState state, int partLength)
        {
            if (partLength <= 0)
            {
                if (partLength < 0)
                {
                    state.StateMachine.Fire(errorTrigger, state.SetError("Invalid attribute format length"));
                }
                else
                {
                    if (state.TestCharLength(1))
                    {
                        if (state.CurrentFormatChar == SPECIAL_CHAR_ATT_END)
                        {
                            state.StateMachine.Fire(doneTrigger, state.SetFormatForCurrentElement(string.Empty).IncrementIndex(1));
                        }
                        else
                        {
                            state.StateMachine.Fire(errorTrigger, state.SetError($"Unknown end of attribute format: {state.CurrentFormatChar}"));
                        }
                    }
                    else
                    {
                        state.StateMachine.Fire(errorTrigger, state.SetError($"Unexpected end of attribute format. Expected '{SPECIAL_CHAR_ATT_END}'"));
                    }
                }
                return;
            }

            var format = state.FormatSubstring(partLength);

            var newState = state.SetFormatForCurrentElement(format.Replace("{}", "{0}")).IncrementIndex(partLength);
            if (newState.TestCharLength(1) && newState.CurrentFormatChar == SPECIAL_CHAR_ATT_END)
            {
                newState.StateMachine.Fire(doneTrigger, newState.IncrementIndex(1));
            }
            else
            {
                newState.StateMachine.Fire(errorTrigger, newState.SetError($"Unexpected end of attribute format. Expected '{SPECIAL_CHAR_ATT_END}'"));
            }
        }

        private void HandleModifer(SMState state)
        {
            if (state.TestCharLength(1))
            {
                var type = state.CurrentFormatChar;

                if (GetSpecialCharsForFlagsEn(SpecialCharFlags.Modifier | SpecialCharFlags.ForAttribute).Contains(type))
                {
                    if (SpecialChars[type].ModifierElement == ModifierElement.ErrorHandler)
                    {
                        state.StateMachine.Fire(invokeTrigger, state.SetPriorState(State.Modifier));
                        return;
                    }
                    var modifier = state.ElementFactory.CreateModifier(SpecialChars[type].ModifierElement);
                    if (modifier == null)
                    {
                        state.StateMachine.Fire(errorTrigger, state.SetError($"Could not create modifier: {type}"));
                    }
                    else
                    {
                        state.StateMachine.Fire(foundModifierTrigger, state.AddModifierForCurrentElement(modifier).IncrementIndex(1));
                    }
                    return;
                }
            }
            state.StateMachine.Fire(doneTrigger, state);
        }

        private void HandleFinalizePart(SMState state)
        {
            if (state.CurrentLogAttribute == SMState.InvalidLogAttribute)
            {
                state.StateMachine.Fire(errorTrigger, state.SetError("Missing log attribute"));
            }
            else if (state.CurrentElementConditionals?.Where(c => c.Type == ConditionalElement.ValidLog || c.Type == ConditionalElement.InvalidLog).Distinct().Count() > 1)
            {
                state.StateMachine.Fire(errorTrigger, state.SetError("Invalid conditional combo. Can't have valid and invalid conditionals at the same time"));
            }
            else
            {
                var conditionals = state.CurrentElementConditionals?.Distinct().ToArray();
                var modifiers = state.CurrentElementModifiers?.Distinct().ToArray();

                IElement element;
                if (state.CurrentLogFormat == null)
                {
                    element = state.ElementFactory.CreateElement(state.CurrentLogAttribute, conditionals: conditionals, modifiers: modifiers);
                }
                else
                {
                    element = state.ElementFactory.CreateElement(state.CurrentLogAttribute, state.CurrentLogFormat, conditionals, modifiers);
                }

                if (element == null)
                {
                    state.StateMachine.Fire(errorTrigger, state.SetError("Could not create element. Please file a ticket with the log format so it can be investigated further"));
                }
                else
                {
                    state.StateMachine.Fire(doneTrigger, state.AddElement(element).ResetCurrentElementValues());
                }
            }
        }

        private void HandleErrorModifierSanityCheck(SMState state)
        {
            if (state.TestCharLength(2))
            {
                if (state.CurrentFormatChar != SPECIAL_CHAR_MOD_ERROR_HANDLER)
                {
                    state.StateMachine.Fire(errorTrigger, state.SetError("Error handler modifier was specified but is not an error handler"));
                    return;
                }
                var newState = state.IncrementIndex(1);
                if (newState.CurrentFormatChar != '"')
                {
                    newState.StateMachine.Fire(errorTrigger, newState.SetError("Missing quoted error string"));
                }
                else
                {
                    newState.StateMachine.Fire(invokeTrigger, newState);
                }
            }
            else
            {
                state.StateMachine.Fire(errorTrigger, state.SetError($"Unexpected end of error handler. Expected {SPECIAL_CHAR_MOD_ERROR_HANDLER}\"<format>\""));
            }
        }

        private void HandleErrorModifier(SMState state)
        {
            //TODO: Done, Error (it's basically parsing a new format and returning the result as the handler)
        }

#endregion
    }
}
