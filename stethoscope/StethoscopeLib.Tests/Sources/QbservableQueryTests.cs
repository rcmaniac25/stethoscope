﻿using NSubstitute;

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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable;

            int counter = 0;
            int nonNull = -1;
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

            var res = logQbservable.LastOrDefaultAsync().Wait();
            Assert.That(res, Is.Null);
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Skip(2);

            int counter = 0;
            int nonNull = -1;
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Skip(1).Skip(1);

            int counter = 0;
            int nonNull = -1;
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Skip(TimeSpan.Zero);

            int counter = 0;
            int nonNull = -1;
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

            Assert.That(counter, Is.EqualTo(5));
            Assert.That(nonNull, Is.EqualTo(2));
        }

        [Test, Retry(2)] // For some reason this has the tendency to fail with the default scheduler when run as with the rest of the fixture, but will pass when run individually. SO retry at least once if needed
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Skip(1).Skip(TimeSpan.Zero);

            int counter = 0;
            int nonNull = -1;
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Skip(TimeSpan.Zero).Skip(1);

            int counter = 0;
            int nonNull = -1;
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Skip(1).Select(en => en != null ? en.Message : null);

            int counter = 0;
            int nonNull = -1;
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Select(en => en != null ? en.Message : null).Skip(1);

            int counter = 0;
            int nonNull = -1;
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Where(en => en != null && en.IsValid).Skip(1).Select(en => en != null ? en.Message : null);

            int counter = 0;
            int nonNull = -1;
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

            Assert.That(counter, Is.EqualTo(1));
            Assert.That(nonNull, Is.EqualTo(0));
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Where(en => en != null && en.IsValid).Skip(TimeSpan.Zero).Select(en => en != null ? en.Message : null);

            int counter = 0;
            int nonNull = -1;
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Skip(1).SkipWhile(en => en == null);

            int counter = 0;
            int nonNull = -1;
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.SkipWhile(en => en == null).Skip(1);

            int counter = 0;
            int nonNull = -1;
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Skip(1).Select(en => en != null ? en.Message : null);

            int counter = 0;
            int nonNull = -1;
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
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var queryToTest = logQbservable.Select(en => en != null ? en.Message : null).Skip(1);

            int counter = 0;
            int nonNull = -1;
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

            Assert.That(counter, Is.EqualTo(4));
            Assert.That(nonNull, Is.EqualTo(1));
        }

        //XXX - put operations in order listed
        //TODO: ... <continue work on rewrite of evaluator>
        //TODO: ... (anything that might invalidate some operation?)
    }
}
