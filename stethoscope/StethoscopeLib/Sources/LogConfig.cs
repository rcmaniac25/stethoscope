using Metrics;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Stethoscope.Common;

using System;
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

    /// <summary>
    /// Per-log/per-log type configuration.
    /// </summary>
    public struct LogConfig : ICloneable
    {
        private static readonly MetricsContext logConfigContext = Metric.Context("LogConfig");
        private static readonly Counter getAttributeCounter = logConfigContext.Counter("Attribute Invocation", Unit.Calls, "config, reflection");
        private static readonly Counter cloneCounter = logConfigContext.Counter("Clone Invocation", Unit.Calls, "config, clone");

        // Opt
        /// <summary>Path to use for getting <see cref="LogAttribute.ThreadID"/></summary>
        public string ThreadIDPath { get; set; }
        /// <summary>Path to use for getting <see cref="LogAttribute.SourceFile"/></summary>
        public string SourceFilePath { get; set; }
        /// <summary>Path to use for getting <see cref="LogAttribute.Function"/></summary>
        public string FunctionPath { get; set; }
        /// <summary>Path to use for getting <see cref="LogAttribute.SourceLine"/></summary>
        public string LogLinePath { get; set; }
        /// <summary>Path to use for getting <see cref="LogAttribute.Level"/></summary>
        public string LogLevelPath { get; set; }
        /// <summary>Path to use for getting <see cref="LogAttribute.SequenceNumber"/></summary>
        public string LogSequencePath { get; set; }
        /// <summary>Path to use for getting <see cref="LogAttribute.Module"/></summary>
        public string ModulePath { get; set; }
        /// <summary>Path to use for getting <see cref="LogAttribute.Type"/></summary>
        public string LogTypePath { get; set; }
        /// <summary>Path to use for getting <see cref="LogAttribute.Section"/></summary>
        public string SectionPath { get; set; }
        /// <summary>Path to use for getting <see cref="LogAttribute.TraceID"/></summary>
        public string TraceIdPath { get; set; }
        /// <summary>Path to use for getting <see cref="LogAttribute.Context"/></summary>
        public string ContextPath { get; set; }

        /// <summary>
        /// How to handle failures during parsing.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public LogParserFailureHandling ParsingFailureHandling { get; set; }
        /// <summary>
        /// If the log data has a root element or not. Ex. <root><logdata/></root> vs. <logdata/>
        /// </summary>
        public bool LogHasRoot { get; set; }
        /// <summary>
        /// Configs to use in addition to the existing configs.
        /// </summary>
        public Dictionary<string, string> ExtraConfigs { get; set; }

        // Req
        /// <summary>Path to use for getting <see cref="LogAttribute.Timestamp"/>. Will always be a <see cref="System.DateTime"/>.</summary>
        public string TimestampPath { get; set; }
        /// <summary>Path to use for getting <see cref="LogAttribute.Message"/>. Will always be a <see cref="System.String"/>.</summary>
        public string LogMessagePath { get; set; }

        /// <summary>
        /// If this config is valid for a <see cref="ILogParser"/> to use.
        /// </summary>
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
        /// Create a clone of the <see cref="LogConfig"/>.
        /// </summary>
        /// <returns>A clone of <see cref="LogConfig"/>.</returns>
        public object Clone()
        {
            cloneCounter.Increment();

            return new LogConfig()
            {
                ThreadIDPath = ThreadIDPath,
                SourceFilePath = SourceFilePath,
                FunctionPath = FunctionPath,
                LogLinePath = LogLinePath,
                LogLevelPath = LogLevelPath,
                LogSequencePath = LogSequencePath,
                ModulePath = ModulePath,
                LogTypePath = LogTypePath,
                SectionPath = SectionPath,
                TraceIdPath = TraceIdPath,
                ContextPath = ContextPath,

                ParsingFailureHandling = ParsingFailureHandling,
                LogHasRoot = LogHasRoot,
                ExtraConfigs = new Dictionary<string, string>(ExtraConfigs),

                TimestampPath = TimestampPath,
                LogMessagePath = LogMessagePath
            };
        }

        /// <summary>
        /// Get all attributes and their associated variable names, for use by reflection.
        /// </summary>
        /// <returns>IDictionary of all attributes and their associated variable name.</returns>
        public static IDictionary<LogAttribute, string> GetAttributePaths()
        {
            getAttributeCounter.Increment();

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
