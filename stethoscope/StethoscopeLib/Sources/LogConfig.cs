using Newtonsoft.Json;

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
    }
}
