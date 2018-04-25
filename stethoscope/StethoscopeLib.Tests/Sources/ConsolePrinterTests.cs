﻿using System;
using System.IO;

using LogTracker.Printers.Internal;

using NUnit.Framework;

namespace LogTracker.Tests.Sources
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

            _fakeStdOut = new StringWriter();
            _fakeStdOut.NewLine = "\n";
            Console.SetOut(_fakeStdOut);

            base.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            Console.SetOut(_originalStdOut);
        }

        private string GetConsoleOutput(bool stripEndingNewline = true)
        {
            var str = _fakeStdOut.ToString();
            if (stripEndingNewline && str.EndsWith(_fakeStdOut.NewLine))
            {
                return str.Substring(0, str.Length - 1);
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
