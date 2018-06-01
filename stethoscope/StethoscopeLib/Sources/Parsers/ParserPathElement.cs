namespace LogTracker.Parsers
{
    enum ParserPathElementType
    {
        Unknown,

        DirectNamedField,
        NamedField,
        IndexField,
        FilterField
    }

    enum ParserPathElementFieldType
    {
        Unknown,
        NotAValue,

        Bool,
        Int,
        String,
        KeyValue
    }

    class ParserPathElement
    {
        public ParserPathElementType Type { get; set; }
        public ParserPathElementFieldType FieldType { get; set; }

        public string StringValue { get; set; }
        public int IndexValue { get; set; }
    }
}
