namespace LogTracker
{
    public struct LogConfig
    {
        public string ThreadIDAttributeName { get; set; }
        public string SourceFileAttributeName { get; set; }
        public string FunctionAttributeName { get; set; }
        public string LogLineAttributeName { get; set; }
        public string LogMessagePath { get; set; }

        public bool IsValid
        {
            get
            {
                return !(string.IsNullOrWhiteSpace(ThreadIDAttributeName) || 
                    string.IsNullOrWhiteSpace(SourceFileAttributeName) || 
                    string.IsNullOrWhiteSpace(FunctionAttributeName) || 
                    string.IsNullOrWhiteSpace(LogMessagePath));
            }
        }
    }
}
