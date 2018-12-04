using Stethoscope.Common;
using Stethoscope.Parsers.Internal.XML;

using NSubstitute;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Stethoscope.Tests
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

        //TODO: test ParseAsync functions

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
        public void BasicTestWithRoot()
        {
            logConfig.LogHasRoot = true;

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);

            var log = "<someroot><fakelog time=\"goodtime\" log=\"my log\"></fakelog></someroot>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void BasicTestWithRootMissing()
        {
            logConfig.LogHasRoot = true;

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

        [Test]
        public void ApplyContextConfigContextInvalid()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            Assert.Throws<ArgumentNullException>(() =>
            {
                parser.ApplyContextConfig(null, null);
            });
        }

        [Test]
        public void ApplyContextConfigNullDictionary()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);

            parser.ApplyContextConfig(null, contextParser =>
            {
                Assert.That(Object.ReferenceEquals(contextParser, parser), Is.False);

                var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
                using (var ms = CreateStream(log))
                {
                    contextParser.Parse(ms);
                }
            });

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ApplyContextConfigEmptyDictionary()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);

            var configs = new Dictionary<ContextConfigs, object>();
            parser.ApplyContextConfig(configs, contextParser =>
            {
                Assert.That(Object.ReferenceEquals(contextParser, parser), Is.False);

                var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
                using (var ms = CreateStream(log))
                {
                    contextParser.Parse(ms);
                }
            });

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ApplyContextConfigLogSource()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);

            var configs = new Dictionary<ContextConfigs, object>
            {
                { ContextConfigs.LogSource, "tests" }
            };
            parser.ApplyContextConfig(configs, contextParser =>
            {
                Assert.That(Object.ReferenceEquals(contextParser, parser), Is.False);

                var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
                using (var ms = CreateStream(log))
                {
                    contextParser.Parse(ms);
                }
            });

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.LogSource, "tests");
        }

        [Test]
        public void ApplyContextConfigLogSourceExtension()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);
            
            parser.ApplyContextConfig(ContextConfigs.LogSource, "tests", contextParser =>
            {
                Assert.That(Object.ReferenceEquals(contextParser, parser), Is.False);

                var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
                using (var ms = CreateStream(log))
                {
                    contextParser.Parse(ms);
                }
            });

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.LogSource, "tests");
        }

        [Test]
        public void ApplyContextConfigLogSourceInvalid()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            Assert.Throws<ArgumentException>(() =>
            {
                parser.ApplyContextConfig(ContextConfigs.LogSource, 123, contextParser =>
                {
                });
            });
        }

        [Test]
        public void ApplyContextConfigLogParserFailureHandling()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);
            logRegistry.AddFailedLog().Returns(failedLogEntry);
            
            parser.ApplyContextConfig(ContextConfigs.FailureHandling, LogParserFailureHandling.MarkEntriesAsFailed, contextParser =>
            {
                Assert.That(Object.ReferenceEquals(contextParser, parser), Is.False);

                var log = "<fakelog time=\"goodtime\" log=\"my log1\"></fakelog><fakelog log=\"my log2\"></fakelog>";
                using (var ms = CreateStream(log))
                {
                    contextParser.Parse(ms);
                }
            });

            logRegistry.Received(1).AddLog("goodtime", "my log1");
            logRegistry.Received(1).AddFailedLog();
            logRegistry.Received(1).NotifyFailedLogParsed(failedLogEntry);
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
            logRegistry.Received().AddValueToLog(failedLogEntry, LogAttribute.Message, Arg.Any<object>());
        }

        [Test]
        public void ApplyContextConfigLogParserFailureHandlingInvalid()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            Assert.Throws<ArgumentException>(() =>
            {
                parser.ApplyContextConfig(ContextConfigs.FailureHandling, false, contextParser =>
                {
                });
            });
        }

        [Test]
        public void ApplyContextConfigLogHasRoot()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            parser.ApplyContextConfig(ContextConfigs.LogHasRoot, true, contextParser =>
            {
                Assert.That(Object.ReferenceEquals(contextParser, parser), Is.False);

                var log = "<myroot><fakelog time=\"goodtime\" log=\"my log\"></fakelog></myroot>";
                using (var ms = CreateStream(log))
                {
                    contextParser.Parse(ms);
                }
            });

            logRegistry.Received(1).AddLog("goodtime", "my log");
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ApplyContextConfigLogHasRootInvalid()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            Assert.Throws<ArgumentException>(() =>
            {
                parser.ApplyContextConfig(ContextConfigs.LogHasRoot, "false", contextParser =>
                {
                });
            });
        }

        [Test]
        public void ApplyContextConfigMultiConfig()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);
            logRegistry.AddFailedLog().Returns(failedLogEntry);

            var configs = new Dictionary<ContextConfigs, object>
            {
                { ContextConfigs.LogSource, "tests" },
                { ContextConfigs.FailureHandling, LogParserFailureHandling.MarkEntriesAsFailed }
            };
            parser.ApplyContextConfig(configs, contextParser =>
            {
                Assert.That(Object.ReferenceEquals(contextParser, parser), Is.False);

                var log = "<fakelog time=\"goodtime\" log=\"my log1\"></fakelog><fakelog log=\"my log2\"></fakelog>";
                using (var ms = CreateStream(log))
                {
                    contextParser.Parse(ms);
                }
            });

            logRegistry.Received(1).AddLog("goodtime", "my log1");
            logRegistry.Received(1).AddFailedLog();
            logRegistry.Received(1).NotifyFailedLogParsed(failedLogEntry);
            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.LogSource, "tests");
            logRegistry.Received(2).AddValueToLog(failedLogEntry, Arg.Is<LogAttribute>(att => att == LogAttribute.Message || att == LogAttribute.LogSource), Arg.Any<object>());
        }

        [Test]
        public void ElementPathEmpty()
        {
            logConfig.LogTypePath = "/";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\">Element Data</fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "Element Data");
        }

        [Test]
        public void ElementPathDirectNamed()
        {
            logConfig.LogTypePath = "!type";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\" type=\"Element Data\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "Element Data");
        }

        [Test]
        public void ElementPathDirectNamedMissing()
        {
            logConfig.LogTypePath = "!type";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ElementPathIndex([Random(0, 10, 4, Distinct = true)]int index)
        {
            string[] PotentialValues = Enumerable.Range(0, 10).Select(i => $"Data{i}").ToArray();
            Assert.That(PotentialValues, Has.Length.EqualTo(10));

            logConfig.LogTypePath = $"/#{index}";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);
            
            var log = $"<fakelog time=\"goodtime\" log=\"my log\">{string.Join("", PotentialValues.Select(value => $"<in>{value}</in>"))}</fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, PotentialValues[index]);
        }

        [Test]
        public void ElementPathIndexMissing()
        {
            logConfig.LogTypePath = "/#2";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><ele>Element Data</ele></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ElementPathFilteredCdata()
        {
            logConfig.LogTypePath = "/$cdata";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><![CDATA[CDat Data]]><ele>Element Data</ele>Text Data</fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "CDat Data");
        }

        [Test]
        public void ElementPathFilteredElement([Values("element", "elem")]string pathKeyword)
        {
            logConfig.LogTypePath = $"/${pathKeyword}";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><![CDATA[CDat Data]]><ele>Element Data</ele>Text Data</fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "Element Data");
        }

        [Test]
        public void ElementPathFilteredText()
        {
            logConfig.LogTypePath = "/$text";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><![CDATA[CDat Data]]><ele>Element Data</ele>Text Data</fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "Text Data");
        }

        [Test]
        public void ElementPathFilteredMissing()
        {
            logConfig.LogTypePath = "/$text";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><![CDATA[CDat Data]]><ele>Element Data</ele></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ElementPathNamedNode()
        {
            logConfig.LogTypePath = "/!type";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><type>Element Data</type></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "Element Data");
        }

        [Test]
        public void ElementPathNamedAttribute()
        {
            logConfig.LogTypePath = "/!type";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\" type=\"Element Data\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "Element Data");
        }

        [Test]
        public void ElementPathMultiNamedNode()
        {
            logConfig.LogTypePath = "/!type";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><type>Element Data</type><type>Not</type></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "Element Data");
        }

        [Test]
        public void ElementPathMultiFiltered()
        {
            logConfig.LogTypePath = "/$cdata";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><![CDATA[CDat Data]]><![CDATA[Not]]></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "CDat Data");
        }
        
        [Test]
        public void ElementPathMixedChain()
        {
            logConfig.LogTypePath = "/$elem/#2/!type";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><ele><sub>Nope</sub><sub>Nope</sub><sub><type>MyType</type></sub></ele></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "MyType");
        }

        [Test]
        public void ElementPathMixedChainAttribute()
        {
            logConfig.LogTypePath = "/$elem/#1/!type";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><ele><sub>Nope</sub><sub type=\"Att Data\">Nope</sub><sub type=\"Still Nope\">Nope</sub></ele></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "Att Data");
        }

        [Test]
        public void ElementPathMixedFiltered()
        {
            logConfig.LogTypePath = "/$elem/$cdata";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><ele><sub>Nope</sub></ele><ele><sub>Nope</sub><sub type=\"Not\">Nope</sub><![CDATA[CDat Data]]><sub type=\"Not\">Nope</sub></ele></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "CDat Data");
        }

        [Test]
        public void ElementPathMixedFilteredCdataFail()
        {
            logConfig.LogTypePath = "/$elem/$cdata/!dat";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><ele><sub>Nope</sub></ele><ele><sub>Nope</sub><sub type=\"Not\">Nope</sub><![CDATA[CDat Data]]><sub type=\"Not\">Nope</sub></ele></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void ElementPathAllIndex()
        {
            logConfig.LogTypePath = "/#1/#2/#0/#1";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><ele>Nope</ele><ele><ele>Nope</ele><ele>Nope</ele><ele><ele><ele>Nope</ele><ele>Yes</ele></ele><ele>Nope</ele></ele><ele>Nope</ele></ele><ele>Nope</ele></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }
            
            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "Yes");
        }

        [Test]
        public void ElementPathAllIndexFields()
        {
            logConfig.LogTypePath = "/#1/#2/#0/#1";
            logConfig.ThreadIDPath = "/#1/#1";
            logConfig.LogLinePath = "/#1/#3&int";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog(Arg.Any<string>(), Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"><ele>Nope</ele><ele><ele>Nope</ele><ele>ThreadID</ele><ele><ele><ele>Nope</ele><ele>Type</ele></ele><ele>Nope</ele></ele><ele>1354</ele></ele><ele>Nope</ele></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.Type, "Type");
            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.ThreadID, "ThreadID");
            logRegistry.Received(1).AddValueToLog(logEntry, LogAttribute.SourceLine, 1354);
        }
    }
}
