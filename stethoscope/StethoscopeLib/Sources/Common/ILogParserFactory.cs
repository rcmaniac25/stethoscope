namespace Stethoscope.Common
{
    public interface ILogParserFactory
    {
        ILogParser Create(ILogRegistry registry, LogConfig config);
    }
}
