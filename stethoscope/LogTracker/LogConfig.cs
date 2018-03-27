using Newtonsoft.Json;

namespace LogTracker
{
    public struct LogConfig
    {
        public string ThreadIDPath { get; set; }
        public string SourceFilePath { get; set; }
        public string FunctionPath { get; set; }
        public string LogLinePath { get; set; }

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
