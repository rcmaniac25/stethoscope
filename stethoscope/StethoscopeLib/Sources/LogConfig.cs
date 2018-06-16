using Stethoscope.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Stethoscope
{
    /// <summary>
    /// How failure to parse a log entry should be handled
    /// </summary>
    public enum LogParserFailureHandling
    {
        /// <summary>
        /// Failure to parse a log entry will stop the logger and skip any remaining log entries.
        /// </summary>
        [EnumMember(Value = "stop")]
        StopParsing,
        /// <summary>
        /// Failure to parse a log entry will skip that log entry.
        /// </summary>
        [EnumMember(Value = "skip")]
        SkipEntries,
        /// <summary>
        /// Failure to parse a log entry will result in creating a log entry that is not valid but will contain any entry data it could parse.
        /// If no attributes were able to be parsed, the entry won't be saved as it contains no data of use.
        /// </summary>
        [EnumMember(Value = "mark")]
        MarkEntriesAsFailed
    }

    public struct LogConfig
    {
        // Opt
        public string ThreadIDPath { get; set; }
        public string SourceFilePath { get; set; }
        public string FunctionPath { get; set; }
        public string LogLinePath { get; set; }
        public string LogLevelPath { get; set; }
        public string LogSequencePath { get; set; }
        public string ModulePath { get; set; }
        public string LogTypePath { get; set; }
        public string SectionPath { get; set; }
        public string TraceIdPath { get; set; }
        public string ContextPath { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public LogParserFailureHandling ParsingFailureHandling { get; set; }

        // Req
        public string TimestampPath { get; set; }
        public string LogMessagePath { get; set; }

        [JsonIgnore]
        public bool IsValid
        {
            get
            {
                return !(string.IsNullOrWhiteSpace(TimestampPath) || 
                    string.IsNullOrWhiteSpace(LogMessagePath));
            }
        }
        
        /// <summary>
        /// Get all attributes and their associated variable names, for use by reflection.
        /// </summary>
        /// <returns>IDictionary of all attributes and their associated variable name.</returns>
        public static IDictionary<LogAttribute, string> GetAttributePaths()
        {
            //TODO: record stat about function used
            return new Dictionary<LogAttribute, string>
            {
                { LogAttribute.ThreadID, "ThreadIDPath" },
                { LogAttribute.SourceFile, "SourceFilePath" },
                { LogAttribute.Function, "FunctionPath" },
                { LogAttribute.SourceLine, "LogLinePath" },
                { LogAttribute.Level, "LogLevelPath" },
                { LogAttribute.SequenceNumber, "LogSequencePath" },
                { LogAttribute.Module, "ModulePath" },
                { LogAttribute.Type, "LogTypePath" },
                { LogAttribute.Section, "SectionPath" },
                { LogAttribute.TraceID, "TraceIdPath" },
                { LogAttribute.Context, "ContextPath" },

                { LogAttribute.Timestamp, "TimestampPath" },
                { LogAttribute.Message, "LogMessagePath" }
            };
        }
    }
}
