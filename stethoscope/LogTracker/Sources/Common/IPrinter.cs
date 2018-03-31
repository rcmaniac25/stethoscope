namespace LogTracker.Common
{
    public interface IPrinter
    {
        void Print();

        void Setup();
        void Teardown();

        void SetRegistry(ILogRegistry registry);
        void SetConfig(LogConfig config);
    }
}
