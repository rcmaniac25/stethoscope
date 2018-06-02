namespace Stethoscope.Common
{
    public interface IPrinterFactory
    {
        IPrinter Create(ILogRegistry registry, LogConfig config);
    }
}
