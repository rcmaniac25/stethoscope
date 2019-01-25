using System;
using System.IO;

using Stethoscope.Printers.Internal;

using NUnit.Framework;

namespace Stethoscope.Tests
{
    // Since we're replacing standard out, we can't run this is parallel
    [TestFixture(TestOf = typeof(ConsolePrinter)), NonParallelizable]
    public class ConsolePrinterTests : IOPrinterTests
    {
        private TextWriter _originalStdOut;
        private StringWriter _fakeStdOut;

        [SetUp]
        public override void Setup()
        {
            _originalStdOut = Console.Out;

            _fakeStdOut = new StringWriter
            {
                NewLine = "\n"
            };
            Console.SetOut(_fakeStdOut);

            base.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            Console.SetOut(_originalStdOut);
        }

        protected override void ResetPrintedData()
        {
            _fakeStdOut.GetStringBuilder().Clear();
        }

        private string GetConsoleOutput(bool stripEndingNewline = true)
        {
            var str = _fakeStdOut.ToString();
            while (stripEndingNewline && str.EndsWith(_fakeStdOut.NewLine)) // Not perfect, but we don't want to start adding newlines in places we don't expect them to be
            {
                str = str.Substring(0, str.Length - 1);
            }
            return str;
        }

        private ConsolePrinter CreateConsolePrinter()
        {
            //XXX Should we just use a factory?

            var printer = new ConsolePrinter();
            printer.SetConfig(logConfig);
            printer.SetRegistry(logRegistry);
            return printer;
        }

        protected override string PrintedDataNewLine
        {
            get
            {
                return _fakeStdOut.NewLine;
            }
        }

        protected override string GetPrintedData()
        {
            return GetConsoleOutput();
        }

        protected override IOPrinter GetIOPrinter()
        {
            return CreateConsolePrinter();
        }

        [Test]
        public void ConsoleSanityTest()
        {
            Console.WriteLine("test msg");
            Assert.That(GetConsoleOutput(), Is.EqualTo("test msg"));
        }
    }
}
