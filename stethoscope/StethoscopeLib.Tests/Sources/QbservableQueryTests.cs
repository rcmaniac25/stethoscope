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
        
        private IQbservable<ILogEntry> SetupListStorageQbservable(IList<ILogEntry> list, IScheduler schedulerToUse)
        {
            mockRegistryStorage.LogScheduler.ReturnsForAnyArgs(schedulerToUse);

            var logs = list.AsListCollection();
            var evaluator = new ListStorageEvaluator(mockRegistryStorage, logs);
            return new EvaluatableQbservable<ILogEntry>(evaluator);
        }

        private string PrintExpression<T>(IQbservable<T> qbservable)
        {
            var expressionString = qbservable.Expression.ToString();
            Console.WriteLine("Original Expression: {0}", expressionString);
            return expressionString;
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
                    return expressionString;
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

        private bool ListStorageTest<T>(IQbservable<T> queryToTest, out int count, out int nonNullIndex)
        {
            var waitSem = new System.Threading.SemaphoreSlim(0);

            int counter = 0;
            int nonNull = -1;
            var originalExpression = PrintExpression(queryToTest);
            var disposable = queryToTest.Subscribe(en =>
            {
                if (en != null)
                {
                    nonNull = counter;
                }

                counter++;
            }, () => waitSem.Release());

            while (!waitSem.Wait(10)) ;

            disposable.Dispose();
            var evaluatedExpression = PrintSubscribedExpression(queryToTest);

            count = counter;
            nonNullIndex = nonNull;

            return originalExpression != evaluatedExpression;
        }

        //TODO: wrap the process of execution and also compare the expression strings to ensure they're different

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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(5));
            Assert.That(nonNull, Is.EqualTo(2));
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

            var originalExpression = PrintExpression(logQbservable);
            var res = logQbservable.LastOrDefaultAsync().Wait();
            var evaluatedExpression = PrintSubscribedExpression(logQbservable);
            Assert.That(res, Is.Null);
            //TODO
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(3));
            Assert.That(nonNull, Is.EqualTo(0));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(3));
            Assert.That(nonNull, Is.EqualTo(0));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(5));
            Assert.That(nonNull, Is.EqualTo(2));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(0));
            Assert.That(nonNull, Is.EqualTo(-1));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(1));
            Assert.That(nonNull, Is.EqualTo(0));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(3));
            Assert.That(nonNull, Is.EqualTo(0));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(2));
            Assert.That(nonNull, Is.EqualTo(-1));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
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

            ListStorageTest(queryToTest, out int counter, out int nonNull);
            
            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
        }

        //XXX - put operations in order listed
        //TODO: ... <continue work on rewrite of evaluator>
        //TODO: ... (anything that might invalidate some operation?)
    }
}
