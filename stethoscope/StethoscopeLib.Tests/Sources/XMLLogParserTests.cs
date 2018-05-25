﻿using LogTracker.Common;
using LogTracker.Parsers.Internal.XML;

using NSubstitute;

using NUnit.Framework;

using System;
using System.IO;
using System.Text;

namespace LogTracker.Tests
{
    [TestFixture(TestOf = typeof(XMLLogParser))]
    public class XMLLogParserTests
    {
        private static readonly Tuple<LogAttribute, Func<LogConfig, string, LogConfig>>[] PossibleConfigs;

        static XMLLogParserTests()
        {
            // Attempt to make things more readable... OMG this is ugly... I really wish this was F#... just for this line. In F# this would be `let makeTuple v1 v2 -> new Tuple<_,_>(v1, v2)`...
            // ...and this would be `PossibleConfigs <- [| makeTuple(LogAttribute.ThreadID, fun conf name -> conf.ThreadIDPath <- printfn "!%s" name; conf)
            //                                             ...
            //                                          |]
            // The worst part is printfn, and that may have been replaced by a similar syntax in newer F# versions. I'm just trying to make this easier to read. Typer inference would be beautiful

            var makeTuple = new Func<LogAttribute, Func<LogConfig, string, LogConfig>, Tuple<LogAttribute, Func<LogConfig, string, LogConfig>>>((type, func) => new Tuple<LogAttribute, Func<LogConfig, string, LogConfig>>(type, func));

            PossibleConfigs = new Tuple<LogAttribute, Func<LogConfig, string, LogConfig>>[]
            {
                makeTuple(LogAttribute.ThreadID, (conf, name) => { conf.ThreadIDPath = $"!{name}"; return conf; }),
                makeTuple(LogAttribute.SourceFile, (conf, name) => { conf.SourceFilePath = $"!{name}"; return conf; }),
                makeTuple(LogAttribute.Function, (conf, name) => { conf.FunctionPath = $"!{name}"; return conf; }),
                makeTuple(LogAttribute.SourceLine, (conf, name) => { conf.LogLinePath = $"!{name}"; return conf; }),
                makeTuple(LogAttribute.Level, (conf, name) => { conf.LogLevelPath = $"!{name}"; return conf; }),
                makeTuple(LogAttribute.SequenceNumber, (conf, name) => { conf.LogSequencePath = $"!{name}"; return conf; }),
                makeTuple(LogAttribute.Module, (conf, name) => { conf.ModulePath = $"!{name}"; return conf; }),
                makeTuple(LogAttribute.Type, (conf, name) => { conf.LogTypePath = $"!{name}"; return conf; }),
                makeTuple(LogAttribute.Section, (conf, name) => { conf.SectionPath = $"!{name}"; return conf; }),
                makeTuple(LogAttribute.TraceID, (conf, name) => { conf.TraceIdPath = $"!{name}"; return conf; }),
                makeTuple(LogAttribute.Context, (conf, name) => { conf.ContextPath = $"!{name}"; return conf; })
            };
        }

        private ILogRegistry logRegistry;
        private ILogEntry logEntry;
        private ILogEntry failedLogEntry;
        private LogConfig logConfig;

        [SetUp]
        public void Setup()
        {
            logRegistry = Substitute.For<ILogRegistry>();
            logEntry = Substitute.For<ILogEntry>();
            failedLogEntry = Substitute.For<ILogEntry>();
            logConfig = new LogConfig()
            {
                TimestampPath = "!time",
                LogMessagePath = "!log"
            };

            SetupLogEntry(logEntry, true);
            SetupLogEntry(failedLogEntry, false);
        }

        private static void SetupLogEntry(ILogEntry entry, bool isValid)
        {
            entry.IsValid.Returns(isValid);
        }

        private static MemoryStream CreateStream(string data)
        {
            var ms = new MemoryStream();

            var dataBytes = Encoding.UTF8.GetBytes(data);
            ms.Write(dataBytes, 0, dataBytes.Length);

            ms.Position = 0L;

            return ms;
        }

        [Test]
        public void BasicTest()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void MultipleBasicLogs()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log1\"></fakelog><fakelog time=\"goodtime\" log=\"my log2\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received().AddLog("goodtime", Arg.Is<string>(x => x.StartsWith("my log")));
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void NoConfig()
        {
            var parser = new XMLLogParser();
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), Arg.Any<string>());
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void NoRegistry()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }
        }

        [Test]
        public void SetConfigInvalidTimestampPath()
        {
            logConfig.TimestampPath = "!";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), Arg.Any<string>());
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void SetConfigInvalidLogMessagePath()
        {
            logConfig.LogMessagePath = "!";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), Arg.Any<string>());
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void VariableTest()
        {
            Assert.That(PossibleConfigs.Length, Is.EqualTo(Enum.GetValues(typeof(LogAttribute)).Length - 3)); // Don't count message, timestamp, or LogSource
        }

        [TestCaseSource("PossibleConfigs")]
        public void SetConfigAdditionalAttribute(Tuple<LogAttribute, Func<LogConfig, string, LogConfig>> attr)
        {
            var config = attr.Item2(logConfig, "custAttr");

            var parser = new XMLLogParser();
            parser.SetConfig(config);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);
            logRegistry.AddValueToLog(logEntry, attr.Item1, "colorful").Returns(true);

            var log = "<fakelog time=\"goodtime\" log=\"my log\" custAttr=\"colorful\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.Received().AddValueToLog(logEntry, attr.Item1, "colorful");
        }

        [Test]
        public void SetConfigEmptyAttribute()
        {
            logConfig.LogTypePath = "";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);
            logRegistry.AddValueToLog(logEntry, LogAttribute.Type, "nope").Returns(true);

            var log = "<fakelog time=\"goodtime\" log=\"my log\" type=\"nope\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void SetConfigInvalidAttribute()
        {
            logConfig.LogTypePath = "!";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);
            logRegistry.AddValueToLog(logEntry, LogAttribute.Type, "nope").Returns(true);

            var log = "<fakelog time=\"goodtime\" log=\"my log\" type=\"nope\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void SetConfigValidAttribute()
        {
            logConfig.LogTypePath = "!type";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);
            logRegistry.AddValueToLog(logEntry, LogAttribute.Type, "nope").Returns(true);

            var log = "<fakelog time=\"goodtime\" log=\"my log\" type=\"nope\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.Received().AddValueToLog(logEntry, LogAttribute.Type, "nope");
        }

        [Test]
        public void ParseNullLog()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            Assert.Catch(() => parser.Parse((Stream)null));

            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public void ParseNoLog()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            var log = "";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public void ParseNoInvalidLog()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            var log = "<chocolate";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public void ParseFailureStopIsDefault()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log1\"></fakelog><fakelog log=\"my log2\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddLog("goodtime", "my log1");
            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), "my log2");
            logRegistry.DidNotReceive().AddFailedLog();
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ParseFailureStop()
        {
            logConfig.ParsingFailureHandling = LogParserFailureHandling.StopParsing;

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log1\"></fakelog><fakelog log=\"my log2\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddLog("goodtime", "my log1");
            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), "my log2");
            logRegistry.DidNotReceive().AddFailedLog();
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ParseFailureStopReversed()
        {
            logConfig.ParsingFailureHandling = LogParserFailureHandling.StopParsing;

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog log=\"my log1\"></fakelog><fakelog log=\"my log2\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), Arg.Any<string>());
            logRegistry.DidNotReceive().AddFailedLog();
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ParseFailureSkip()
        {
            logConfig.ParsingFailureHandling = LogParserFailureHandling.SkipEntries;

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log1\"></fakelog><fakelog log=\"my log2\"></fakelog><fakelog time=\"goodtime\" log=\"my log3\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddLog("goodtime", "my log1");
            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), "my log2");
            logRegistry.Received(1).AddLog("goodtime", "my log3");
            logRegistry.DidNotReceive().AddFailedLog();
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ParseFailureSkipReversed()
        {
            logConfig.ParsingFailureHandling = LogParserFailureHandling.SkipEntries;

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog log=\"my log1\"></fakelog><fakelog time=\"goodtime\" log=\"my log2\"></fakelog><fakelog time=\"goodtime\" log=\"my log3\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), "my log1");
            logRegistry.Received(1).AddLog("goodtime", "my log2");
            logRegistry.Received(1).AddLog("goodtime", "my log3");
            logRegistry.DidNotReceive().AddFailedLog();
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ParseFailureHandle()
        {
            logConfig.ParsingFailureHandling = LogParserFailureHandling.MarkEntriesAsFailed;

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);
            logRegistry.AddFailedLog().Returns(failedLogEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log1\"></fakelog><fakelog log=\"my log2\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddLog("goodtime", "my log1");
            logRegistry.Received(1).AddFailedLog();
            logRegistry.Received(1).NotifyFailedLogParsed(failedLogEntry);
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
            logRegistry.Received().AddValueToLog(failedLogEntry, LogAttribute.Message, Arg.Any<object>());
        }

        [Test]
        public void ParseFailureHandleReversed()
        {
            logConfig.ParsingFailureHandling = LogParserFailureHandling.MarkEntriesAsFailed;

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);
            logRegistry.AddFailedLog().Returns(failedLogEntry);

            var log = "<fakelog log=\"my log1\"></fakelog><fakelog time=\"goodtime\" log=\"my log2\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddFailedLog();
            logRegistry.Received(1).AddLog("goodtime", "my log2");
            logRegistry.Received(1).NotifyFailedLogParsed(failedLogEntry);
            logRegistry.Received().AddValueToLog(failedLogEntry, LogAttribute.Message, Arg.Any<object>());
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ParseFailureHandleEmptyEntry()
        {
            logConfig.ParsingFailureHandling = LogParserFailureHandling.MarkEntriesAsFailed;

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);
            logRegistry.AddFailedLog().Returns(failedLogEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log1\"></fakelog><fakelog log=\"my log2\"></fakelog><fakelog></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddLog("goodtime", "my log1");
            logRegistry.Received(2).AddFailedLog();
            logRegistry.Received(2).NotifyFailedLogParsed(failedLogEntry);
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
            logRegistry.Received(1).AddValueToLog(failedLogEntry, LogAttribute.Message, Arg.Any<object>());
        }

        [Test]
        public void LogFile()
        {
            var tmpLogFile = Path.GetTempFileName();
            using (var sw = new StreamWriter(tmpLogFile))
            {
                sw.Write("<fakelog time=\"goodtime-file\" log=\"my log\"></fakelog>");
            }

            Console.WriteLine(tmpLogFile);

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime-file", "my log").Returns(logEntry);

            parser.Parse(tmpLogFile);

            logRegistry.Received().AddLog("goodtime-file", "my log");
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        //TODO: ApplyContextConfig
        //void ApplyContextConfig(IDictionary<ContextConfigs, object> config, Action<ILogParser> context);
        //public static void ApplyContextConfig(this ILogParser parser, ContextConfigs configName, object configValue, Action<ILogParser> context)

        //TODO: Parse: test many different parse paths to ensure they are parsed properly
    }
}
