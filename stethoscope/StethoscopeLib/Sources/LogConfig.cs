using LogTracker.Common;

using Newtonsoft.Json;

using System.Collections.Generic;

namespace LogTracker
{
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

        //TODO: log parse error handling: stop parsing log on bad entry, skip bad log entries, parse as much of bad log entry (marked as bad)

        /// <summary>
        /// Get all attributes and their associated variable names, for use by reflection.
        /// </summary>
        /// <returns>IDictionary of all attributes and their associated variable name.</returns>
        public static IDictionary<LogAttribute, string> GetAttributePaths()
        {
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
