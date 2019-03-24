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

                CurrentElementConditionals = null;
                CurrentLogAttribute = InvalidLogAttribute;
                CurrentLogFormat = null;

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
                var cloneElements = Elements != null ? new List<IElement>(Elements) : null;
                var cloneLogConditionals = Elements != null ? new List<IConditional>(LogConditionals) : null;

                return new SMState()
                {
                    Format = Format,
                    ElementFactory = ElementFactory,
                    ParsingIndex = ParsingIndex,

                    ErrorMessage = ErrorMessage,

                    CurrentElementConditionals = cloneCurrentElementConditionals,
                    CurrentLogAttribute = CurrentLogAttribute,
                    CurrentLogFormat = CurrentLogFormat,

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
            Error,

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
            FinalizePart,

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

        private static readonly Dictionary<char, SpecialCharValues> SpecialChars = new Dictionary<char, SpecialCharValues>()
        {
            { '+', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForLog | SpecialCharFlags.ForAttribute | SpecialCharFlags.StartAttribute, ConditionalElement.ValidLog) },
            { '-', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForLog | SpecialCharFlags.ForAttribute | SpecialCharFlags.StartAttribute, ConditionalElement.InvalidLog) },
            { '^', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForAttribute | SpecialCharFlags.StartAttribute, ConditionalElement.AttributeExists) },
            { '$', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForAttribute | SpecialCharFlags.StartAttribute, ConditionalElement.AttributeValueChanged) },
            { '~', SpecialCharValues.Create(SpecialCharFlags.Conditional | SpecialCharFlags.ForAttribute | SpecialCharFlags.StartAttribute, ConditionalElement.AttributeValueNew) },

            { '!', SpecialCharValues.Create(SpecialCharFlags.Modifier | SpecialCharFlags.ForAttribute, ModifierElement.ErrorHandler) },

            { '{', SpecialCharValues.Create(SpecialCharFlags.Attribute | SpecialCharFlags.StartAttribute) },
            { '}', SpecialCharValues.Create(SpecialCharFlags.Attribute | SpecialCharFlags.EndAttribute) }
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
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState, char[], CountTillFlag> countTillMarkerTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState, char[], CountTillFlag>(Trigger.Invoke);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> doneTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Done);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState, int> doneIntTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState, int>(Trigger.Done);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> errorTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.Error);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState, int> processRawTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState, int>(Trigger.ProcessRaw);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<SMState> processAttributeTrigger = new StateMachine<State, Trigger>.TriggerWithParameters<SMState>(Trigger.ProcessAttribute);
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
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.Part)
                .OnEntryFrom(doneTrigger, state => state.StateMachine.Fire(countTillMarkerTrigger, state, GetSpecialCharsForFlags(SpecialCharFlags.StartAttribute), CountTillFlag.LastMarker))
                .OnEntryFrom(doneIntTrigger, HandlePart)
                .Permit(Trigger.Invoke, State.CountTillMarker)
                .Permit(Trigger.ProcessRaw, State.Raw)
                .Permit(Trigger.ProcessAttribute, State.Attribute)
                .Permit(Trigger.Done, State.Done)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.CountTillMarker)
                .OnEntryFrom(countTillMarkerTrigger, CountTillMarker)
                .Permit(Trigger.Done, State.Part);

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
                .Permit(Trigger.Done, State.AttributeReference);

            machine.Configure(State.AttributeReference)
                .OnEntryFrom(doneTrigger, state => state.StateMachine.Fire(countTillMarkerTrigger, state, GetSpecialCharsForFlagsEn(SpecialCharFlags.EndAttribute).Concat(new char[] { '|' }).ToArray(), CountTillFlag.FirstMarker))
                .OnEntryFrom(doneIntTrigger, HandleAttributeReference)
                .Permit(Trigger.Invoke, State.CountTillMarker)
                .Permit(Trigger.Done, State.Modifier)
                .Permit(Trigger.Invoke, State.AttributeFormat)
                .Permit(Trigger.Error, State.Error);

            machine.Configure(State.AttributeFormat)
                .OnEntryFrom(invokeTrigger, state => state.StateMachine.Fire(countTillMarkerTrigger, state, GetSpecialCharsForFlags(SpecialCharFlags.EndAttribute), CountTillFlag.LastMarker))
                .OnEntryFrom(doneIntTrigger, HandleAttributeFormat)
                .Permit(Trigger.Invoke, State.CountTillMarker)
                .Permit(Trigger.Done, State.Modifier)
                .Permit(Trigger.Error, State.Error);

            //TODO

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

            return machine;
        }

        #endregion
        
        #region State Entry Operations

        private void HandleLogConditional(SMState state)
        {
            //XXX need to support error handler conditional
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
                if (count % 2 == 1 && GetSpecialCharsForFlagsEn(SpecialCharFlags.Conditional | SpecialCharFlags.ForLog).Contains(type))
                {
                    if (state.LogConditionals?.Count != 0)
                    {
                        state.StateMachine.Fire(errorTrigger, state.SetError("Only one log-level conditional is allowed"));
                        return;
                    }
                    var conditional = state.ElementFactory.CreateConditional(SpecialChars[type].ConditionalElement);
                    state.StateMachine.Fire(foundConditionalTrigger, state.AddLogConditional(conditional).IncrementIndex(1));
                    return;
                }
            }
            state.StateMachine.Fire(doneTrigger, state);
        }

        private enum CountTillFlag
        {
            /// <summary>
            /// Stop at the first marker. So ^ (raw = ^^) of "abc^^^" would return 3
            /// </summary>
            FirstMarker,
            /// <summary>
            /// Stop at the last marker. So ^ (raw = ^^) of "abc^^^" would return 5
            /// </summary>
            LastMarker
        }

        private static int? CountTillMarkerCalculateLength(SMState state, int testLen, char c, CountTillFlag flag)
        {
            var notRaw = true;
            if (SpecialChars.ContainsKey(c))
            {
                var innerCount = 1;
                while (state.TestCharLength(innerCount + testLen) && state.Format[state.ParsingIndex + innerCount + testLen - 1] == c)
                {
                    innerCount++;
                }
                if (innerCount % 2 == 0 ||
                    flag == CountTillFlag.LastMarker)
                {
                    notRaw = false;
                    testLen += innerCount - 1;
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
                    var finalLen = CountTillMarkerCalculateLength(state, testLen, c, flag);
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
                state.StateMachine.Fire(doneTrigger, state.AddElement(rawElement).IncrementIndex(partLength));
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

                // Need to test that we're not looking at a raw that is using the special chars (+ means conditional, ++ means it's a raw char of '+')
                // As such, only passes if an odd number of chars match. + = conditional, ++ = raw, +++ = conditional + raw, ++++ = 2x raw, etc.
                var count = 1;
                while (state.TestCharLength(count + 1) && state.Format[state.ParsingIndex + count] == type)
                {
                    count++;
                }
                if (count % 2 == 1 && GetSpecialCharsForFlagsEn(SpecialCharFlags.Conditional | SpecialCharFlags.ForAttribute).Contains(type))
                {
                    var conditional = state.ElementFactory.CreateConditional(SpecialChars[type].ConditionalElement);
                    state.StateMachine.Fire(foundConditionalTrigger, state.AddConditionalForCurrentElement(conditional).IncrementIndex(1));
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
            else if (state.CurrentFormatChar != '{')
            {
                state.StateMachine.Fire(errorTrigger, state.SetError("Attribute reference has no starting '{'"));
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
                newState.StateMachine.Fire(newState.CurrentFormatChar == '|' ? invokeTrigger : doneTrigger, newState.IncrementIndex(1));
            }
            else
            {
                newState.StateMachine.Fire(errorTrigger, newState.SetError("Unexpected end of attribute. Expected '}'"));
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
                        if (state.CurrentFormatChar == '}')
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
                        state.StateMachine.Fire(errorTrigger, state.SetError("Unexpected end of attribute format. Expected '}'"));
                    }
                }
                return;
            }

            var format = state.FormatSubstring(partLength);

            var newState = state.SetFormatForCurrentElement(format.Replace("{}", "{0}")).IncrementIndex(partLength);
            if (newState.TestCharLength(1) && newState.CurrentFormatChar == '}')
            {
                newState.StateMachine.Fire(doneTrigger, newState.IncrementIndex(1));
            }
            else
            {
                newState.StateMachine.Fire(errorTrigger, newState.SetError("Unexpected end of attribute format. Expected '}'"));
            }
        }

        #endregion
    }
}
