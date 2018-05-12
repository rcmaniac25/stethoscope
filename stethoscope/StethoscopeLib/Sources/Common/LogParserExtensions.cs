using System.IO;

namespace LogTracker.Common
{
    public static class LogParserExtensions
    {
        public static void Parse(this ILogParser parser, string logFile)
        {
            using (var fr = new FileStream(logFile, FileMode.Open))
            {
                parser.Parse(fr);
            }
        }
    }
}
