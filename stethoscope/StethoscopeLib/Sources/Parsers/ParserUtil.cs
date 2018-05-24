﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogTracker.Parsers
{
    enum LogParserErrors
    {
        OK,

        ConfigNotInitialized,

        RegistryNotSet, // Optional, since the log parser could simply be doing a pass to try and parse data

        MissingTimestamp,
        MissingMessage,

        ConfigValueInvalid // Do I go in more detail somewhere?
    }

    static class ParserUtil
    {
        private const char NAMED_MARKER = '!';
        private const char INDEX_MARKER = '#';
        private const char FILTER_MARKER = '$';
        private const char TYPE_MARKER = '&';

        #region Cast (Key-Value)

        private static char[] KV_DELIMITERS = new char[] { ';', ',' }; //XXX config value for what to use for splits?
        private const char KV_SEPERATOR = '='; //XXX config?

        private static IEnumerable<Tuple<bool, string>> QuoteGroup(string rawValue)
        {
            var lastChar = '\0';
            var processingQuote = false;
            var firstExecution = true;
            var builder = new StringBuilder();
            foreach (var c in rawValue)
            {
                if (c == '"' && lastChar != '\\')
                {
                    if (!firstExecution)
                    {
                        yield return Tuple.Create(processingQuote, builder.ToString());
                    }
                    builder.Length = 0;
                    processingQuote = !processingQuote;
                }
                else
                {
                    builder.Append(c);
                }
                firstExecution = false;
                lastChar = c;
            }
            if (builder.Length > 0)
            {
                // Theory that we're in a quoted string... but never have an ending quote. Simply use the processingQuote to determine what to return.
                // If we were in a quote, we have no way of knowing if it was supposed to be a quoted string or not.
                yield return Tuple.Create(processingQuote, builder.ToString());
            }
        }

        private static IEnumerable<Tuple<bool, string>> GroupSplitExtractGroup(IEnumerator<Tuple<bool, string>> groupEnumerator, Tuple<bool, string>[] leftover, int priorIndex)
        {
            var remainsIndex = (priorIndex + 1) % 2;

            if (leftover[priorIndex].Item2 != null)
            {
                yield return leftover[priorIndex];
            }
            while (groupEnumerator.MoveNext())
            {
                var group = groupEnumerator.Current;
                if (group.Item1)
                {
                    yield return group;
                }
                else
                {
                    var index = group.Item2.IndexOfAny(KV_DELIMITERS);
                    if (index >= 0)
                    {
                        if (index > 0)
                        {
                            yield return Tuple.Create(group.Item1, group.Item2.Substring(0, index));
                        }
                        leftover[remainsIndex] = Tuple.Create(group.Item1, group.Item2.Substring(index + 1));
                        yield break;
                    }
                    else
                    {
                        yield return group;
                    }
                }
            }

            leftover[remainsIndex] = null;
        }

        // Note: possibly not thread safe (because of the "leftover" data). Don't multi-thread this iteration.
        private static IEnumerable<IEnumerable<Tuple<bool, string>>> GroupSplit(string rawValue)
        {
            var groups = QuoteGroup(rawValue).GetEnumerator();

            // This is a workaround for iterators not being able to use out or ref. It's probably a bad idea...
            var priorIndex = 0;
            var leftover = new Tuple<bool, string>[2];
            leftover[0] = new Tuple<bool, string>(false, null);

            while (leftover[priorIndex] != null)
            {
                yield return GroupSplitExtractGroup(groups, leftover, priorIndex);
                priorIndex = (priorIndex + 1) % 2;
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> QuotedCastKeyValueSplit(string rawValue)
        {
            StringBuilder[] buffers = null;

            foreach (var group in GroupSplit(rawValue))
            {
                buffers = new StringBuilder[2];

                buffers[0] = new StringBuilder();
                buffers[1] = new StringBuilder();

                var bufferIndex = 0;

                foreach (var value in group)
                {
                    if (value.Item1)
                    {
                        buffers[bufferIndex].Append(value.Item2);
                    }
                    else
                    {
                        var sepIndex = value.Item2.IndexOf(KV_SEPERATOR);
                        if (bufferIndex == 0 && sepIndex >= 0)
                        {
                            buffers[bufferIndex].Append(value.Item2.Substring(0, sepIndex));
                            bufferIndex++;
                            buffers[bufferIndex].Append(value.Item2.Substring(sepIndex + 1));
                        }
                        else
                        {
                            buffers[bufferIndex].Append(value.Item2);
                        }
                    }
                }

                if (buffers[0].Length > 0 || buffers[1].Length > 0)
                {
                    yield return new KeyValuePair<string, string>(buffers[0].ToString(), buffers[1].ToString());
                    buffers[0].Length = 0;
                    buffers[1].Length = 0;
                }
            }

            if (buffers != null && (buffers[0].Length > 0 || buffers[1].Length > 0))
            {
                yield return new KeyValuePair<string, string>(buffers[0].ToString(), buffers[1].ToString());
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> FastCastKeyValueSplit(string rawValue)
        {
            return from value in rawValue.Split(KV_DELIMITERS, StringSplitOptions.RemoveEmptyEntries)
                   where value.IndexOf(KV_SEPERATOR) > 0
                   select new KeyValuePair<string, string>(value.Substring(0, value.IndexOf(KV_SEPERATOR)), value.Substring(value.IndexOf(KV_SEPERATOR) + 1));
        }

        private static IEnumerable<KeyValuePair<string, string>> CastKeyValueSplit(string rawValue)
        {
            if (rawValue.IndexOf('"') < 0)
            {
                return FastCastKeyValueSplit(rawValue);
            }
            return QuotedCastKeyValueSplit(rawValue);
        }

        #endregion

        public static object CastField(string rawValue, ParserPathElementFieldType fieldType)
        {
            switch (fieldType)
            {
                case ParserPathElementFieldType.Bool:
                    bool bValue;
                    if (bool.TryParse(rawValue, out bValue))
                    {
                        return bValue;
                    }
                    break;
                case ParserPathElementFieldType.String:
                    return rawValue;
                case ParserPathElementFieldType.Int:
                    int iValue;
                    if (int.TryParse(rawValue, out iValue))
                    {
                        return iValue;
                    }
                    break;
                case ParserPathElementFieldType.KeyValue:
                    if (rawValue == null)
                    {
                        return null;
                    }
                    IDictionary<string, string> kv = new Dictionary<string, string>();
                    foreach (var pair in CastKeyValueSplit(rawValue))
                    {
                        kv.Add(pair);
                    }
                    if (kv.Count > 0)
                    {
                        return kv;
                    }
                    break;
            }
            return null;
        }

        private static ParserPathElementFieldType ParseFieldType(string path)
        {
            var index = path.LastIndexOf(TYPE_MARKER);
            if (index <= 0)
            {
                return ParserPathElementFieldType.String;
            }
            var fieldType = path.Substring(index + 1);
            if (string.IsNullOrWhiteSpace(fieldType))
            {
                return ParserPathElementFieldType.String;
            }
            switch (fieldType.ToLower())
            {
                case "str":
                case "string":
                    return ParserPathElementFieldType.String;
                case "int":
                    return ParserPathElementFieldType.Int;
                case "bool":
                case "boolean":
                    return ParserPathElementFieldType.Bool;
                case "kv":
                    return ParserPathElementFieldType.KeyValue;
            }
            return ParserPathElementFieldType.Unknown;
        }

        private static string StripType(string value)
        {
            var strIndex = value.LastIndexOf(TYPE_MARKER);
            if (strIndex > 0)
            {
                return value.Substring(0, strIndex);
            }
            return value;
        }

        public static ParserPathElement[] ParsePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var elements = new List<ParserPathElement>();

            if (path[0] == NAMED_MARKER)
            {
                if (path.TrimEnd().Length == 1)
                {
                    // Only has the '!' marker
                    return null;
                }
                elements.Add(new ParserPathElement()
                {
                    Type = ParserPathElementType.NamedField,
                    FieldType = ParseFieldType(path),

                    StringValue = StripType(path.Substring(1))
                });
            }
            else
            {
                var sectionsArr = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var sections = sectionsArr.Select((value, index) =>
                {
                    if (index == (sectionsArr.Length - 1))
                    {
                        return StripType(value);
                    }
                    return value;
                });
                foreach (var section in sections)
                {
                    if (section.TrimEnd().Length == 1)
                    {
                        // Only has the marker
                        return null;
                    }
                    if (section[0] == INDEX_MARKER)
                    {
                        if (!int.TryParse(section.Substring(1), out int value) || value < 0)
                        {
                            // Is not an int or is a negative value
                            return null;
                        }
                        elements.Add(new ParserPathElement()
                        {
                            Type = ParserPathElementType.IndexField,
                            FieldType = ParserPathElementFieldType.NotAValue,

                            IndexValue = value
                        });
                    }
                    else if (section[0] == FILTER_MARKER)
                    {
                        elements.Add(new ParserPathElement()
                        {
                            Type = ParserPathElementType.FilterField,
                            FieldType = ParserPathElementFieldType.NotAValue,

                            StringValue = section.Substring(1)
                        });
                    }
                    else if (section[0] == NAMED_MARKER)
                    {
                        elements.Add(new ParserPathElement()
                        {
                            Type = ParserPathElementType.NamedField,
                            FieldType = ParserPathElementFieldType.NotAValue,

                            StringValue = section.Substring(1)
                        });
                    }
                    else
                    {
                        // Don't know what this is
                        return null;
                    }
                }
                if (elements.Count > 0)
                {
                    elements.Last().FieldType = ParseFieldType(path);
                }
            }

            return elements.ToArray();
        }

        public static bool IsFatal(LogParserErrors error, bool isRegistryRequired = true)
        {
            switch (error)
            {
                case LogParserErrors.ConfigNotInitialized:
                case LogParserErrors.ConfigValueInvalid:
                    return true;
                case LogParserErrors.RegistryNotSet:
                    if (isRegistryRequired)
                    {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }
    }
}
