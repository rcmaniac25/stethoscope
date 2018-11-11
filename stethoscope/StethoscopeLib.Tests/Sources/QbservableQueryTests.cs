using NSubstitute;

using NUnit.Framework;

using Stethoscope.Collections;
using Stethoscope.Common;
using Stethoscope.Log.Internal;
using Stethoscope.Log.Internal.Storage.Linq;
using Stethoscope.Reactive.Linq;

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Stethoscope.Tests
{
    [TestFixture]
    public class QbservableQueryTests
    {
        private enum ExpressionStringComparision
        {
            Unknown,

            Same,
            Different
        }

        private readonly Metrics.Timer ExpressionEvaluationTimer = Metrics.Metric.Context("QbservableQueryTests").Timer("Expression Timer", Metrics.Unit.Calls, tags: "test, qbservable, expression");

        private IRegistryStorage mockRegistryStorage;
        private ILogEntry mockLogEntry;

        private static readonly IScheduler[] SchedulersToTest = new IScheduler[]
        {
            DefaultScheduler.Instance, // Regular scheduler
            TaskPoolScheduler.Default  // Long scheduler
        };

        [SetUp]
        public void Setup()
        {
            mockRegistryStorage = Substitute.For<IRegistryStorage>();
            mockLogEntry = Substitute.For<ILogEntry>();

            mockLogEntry.IsValid.ReturnsForAnyArgs(true);
        }

        private static string GetSchedulerName(IScheduler scheduler)
        {
            if (scheduler == null)
            {
                throw new ArgumentNullException(nameof(scheduler));
            }
            Assert.That(SchedulersToTest.Length, Is.EqualTo(2));
            if (scheduler == SchedulersToTest[0])
            {
                return "Default";
            }
            else if (scheduler == SchedulersToTest[1])
            {
                return "TaskPool";
            }
            return "Unknown";
        }

        private IQbservable<ILogEntry> SetupListStorageQbservable(IList<ILogEntry> list, IScheduler schedulerToUse)
        {
            mockRegistryStorage.LogScheduler.ReturnsForAnyArgs(schedulerToUse);

            var logs = list.AsListCollection();
            var evaluator = new ListStorageEvaluator(mockRegistryStorage, logs);
            return new EvaluatableQbservable<ILogEntry>(evaluator);
        }

        private string SubstitueQbservableStrings(string expressionString)
        {
            int index = -1;
            do
            {
                var endChar = ')';
                index = expressionString.IndexOf("Stethoscope.Reactive.Linq.EvaluatableQbservable");
                if (index < 0)
                {
                    endChar = ']';
                    index = expressionString.IndexOf("Stethoscope.Reactive.LiveListObservable");
                }
                else if (expressionString.LastIndexOf("value(", index) >= 0)
                {
                    index = expressionString.LastIndexOf("value(", index);
                }
                if (index >= 0)
                {
                    int endIndex = expressionString.IndexOf('`', index);
                    if (endIndex > 0)
                    {
                        var tindex = expressionString.IndexOf('[', endIndex + 3);
                        endIndex = expressionString.IndexOf(endChar, endIndex + 1);
                        if (tindex > 0 && tindex < endIndex)
                        {
                            throw new Exception("Dev update: need to support recursive quote removals: example is embedded generics (List`1[List`1[int]])");
                        }
                        if (endIndex > 0)
                        {
                            expressionString = $"{expressionString.Substring(0, index)}TestQbservable{expressionString.Substring(endIndex + 1)}";
                        }
                    }
                }
            } while (index >= 0);
            return expressionString;
        }

        private string PrintExpression<T>(IQbservable<T> qbservable)
        {
            var expressionString = qbservable.Expression.ToString();
            Console.WriteLine("Original Expression: {0}", expressionString);
            return SubstitueQbservableStrings(expressionString);
        }

        private string PrintSubscribedExpression<T>(IQbservable<T> qbservable)
        {
            if (qbservable is EvaluatableQbservable<T> evaluatedExpression)
            {
                var evEx = evaluatedExpression.EvaluatedExpression;
                if (evEx != null)
                {
                    var expressionString = evEx.ToString();
                    Console.WriteLine("Evaluated Expression: {0}", expressionString);
                    return SubstitueQbservableStrings(expressionString);
                }
                else
                {
                    Console.WriteLine("Qbservable has not been subscribed to or run yet");
                    return string.Empty;
                }
            }
            else
            {
                Console.WriteLine("Unknown qbservable. Can't get evaluated expression");
                return null;
            }
        }

        private ExpressionStringComparision CompareExpressionsTest<T>(IQbservable<T> queryToTest, Action<IQbservable<T>, Metrics.Timer> test)
        {
            var originalExpression = PrintExpression(queryToTest);
            test(queryToTest, ExpressionEvaluationTimer);
            var evaluatedExpression = PrintSubscribedExpression(queryToTest);

            if (originalExpression == evaluatedExpression)
            {
                return ExpressionStringComparision.Same;
            }
            return ExpressionStringComparision.Different;
        }

        private ExpressionStringComparision ListStorageTest<T>(IQbservable<T> queryToTest, IScheduler scheduler, out int count, out int nonNullIndex)
        {
            var schedulerName = GetSchedulerName(scheduler);

            var waitSem = new System.Threading.SemaphoreSlim(0);

            int counter = 0;
            int nonNull = -1;
            var result = CompareExpressionsTest(queryToTest, (query, timer) =>
            {
                IDisposable tmpDisp; //XXX
                using (timer.NewContext(schedulerName))
                {
                    tmpDisp = query.Subscribe();
                }
                tmpDisp?.Dispose();

                var disposable = query.Subscribe(en =>
                {
                    if (en != null)
                    {
                        nonNull = counter;
                    }

                    counter++;
                }, () => waitSem.Release());

                while (!waitSem.Wait(10)) ;

                disposable.Dispose();
            });

            count = counter;
            nonNullIndex = nonNull;

            return result;
        }

        [Test]
        public void ListStorageDirect([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable;

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(5));
            Assert.That(nonNull, Is.EqualTo(2));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Same));
        }

        [Test]
        public void ListStorageOperation([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.LastOrDefaultAsync();

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(1));
            Assert.That(nonNull, Is.EqualTo(-1));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Same));
        }

        [Test]
        public void ListStorageSkipInt([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Skip(2);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(3));
            Assert.That(nonNull, Is.EqualTo(0));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Different));
        }

        [Test]
        public void ListStorageDoubleSkipInt([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Skip(1).Skip(1);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(3));
            Assert.That(nonNull, Is.EqualTo(0));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Different));
        }

        [Test]
        public void ListStorageSkipTime([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Skip(TimeSpan.Zero);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(5));
            Assert.That(nonNull, Is.EqualTo(2));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Same));
        }

        [Test]
        public void ListStorageDoubleSkip([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Skip(1).Skip(TimeSpan.Zero);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Different));
        }

        [Test]
        public void ListStorageDoubleSkipReversed([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Skip(TimeSpan.Zero).Skip(1);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Same));
        }

        [Test]
        public void ListStorageSkipAndOther([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Skip(1).Select(en => en != null ? en.Message : null);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Different));
        }

        [Test]
        public void ListStorageSkipAndOtherReversed([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Select(en => en != null ? en.Message : null).Skip(1);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Same));
        }

        [Test]
        public void ListStorageSkipSandwich([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Where(en => en != null && en.IsValid).Skip(1).Select(en => en != null ? en.Message : null);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(0));
            Assert.That(nonNull, Is.EqualTo(-1));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Same));
        }

        [Test]
        public void ListStorageAltSkipSandwich([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Where(en => en != null && en.IsValid).Skip(TimeSpan.Zero).Select(en => en != null ? en.Message : null);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(1));
            Assert.That(nonNull, Is.EqualTo(0));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Same));
        }

        [Test]
        public void ListStorageLambdaSkip([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Skip(1).SkipWhile(en => en == null);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(3));
            Assert.That(nonNull, Is.EqualTo(0));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Different));
        }

        [Test]
        public void ListStorageLambdaSkipReversed([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.SkipWhile(en => en == null).Skip(1);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(2));
            Assert.That(nonNull, Is.EqualTo(-1));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Same));
        }

        [Test]
        public void ListStorageLambda([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Skip(1).Select(en => en != null ? en.Message : null);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Different));
        }

        [Test]
        public void ListStorageLambdaReversed([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<ILogEntry>()
            {
                null,
                null,
                mockLogEntry,
                null,
                null
            };

            var logQbservable = SetupListStorageQbservable(list, scheduler);

            var queryToTest = logQbservable.Select(en => en != null ? en.Message : null).Skip(1);

            var comp = ListStorageTest(queryToTest, scheduler, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
            Assert.That(comp, Is.EqualTo(ExpressionStringComparision.Same));
        }

        //XXX - put operations in order listed
        //TODO: ... (anything that might invalidate some operation?)
    }
}
