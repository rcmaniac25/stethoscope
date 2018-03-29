using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                case "int":
                    return ParserPathElementFieldType.Int;
                case "bool":
                    return ParserPathElementFieldType.Bool;
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
                        int value;
                        if (!int.TryParse(section.Substring(1), out value) || value < 0)
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
    }
}
