using System.IO;

namespace LogTracker.Common
{
    public interface ILogParser
    {
        void Parse(Stream logStream);
    }
}
