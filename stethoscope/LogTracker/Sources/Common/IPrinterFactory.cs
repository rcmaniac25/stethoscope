namespace LogTracker.Common
{
    public interface IPrinterFactory
    {
        IPrinter Create(ILogRegistry registry, LogConfig config);
    }
}
