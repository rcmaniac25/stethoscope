using Metrics;

using NUnit.Framework;

using System;
using System.IO;
using System.Threading;

namespace Stethoscope.Tests
{
    [SetUpFixture]
    public class TestSetup
    {
        [OneTimeSetUp]
        public void Setup()
        {
            var curDir = Environment.CurrentDirectory;
            var perfPath = Path.Combine(curDir, "perf-report.txt");
            Metric.Config.WithAllCounters().WithReporting(report => report.WithTextFileReport(perfPath, TimeSpan.FromMilliseconds(500)));
        }

        [OneTimeTearDown]
        public void Shutdown()
        {
            // This is to ensure the report is recorded
            Thread.Sleep(TimeSpan.FromMilliseconds(501));
        }
    }
}
