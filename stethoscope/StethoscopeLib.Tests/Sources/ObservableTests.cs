using NSubstitute;

using NUnit.Framework;

using Stethoscope.Collections;
using Stethoscope.Common;
using Stethoscope.Log.Internal;
using Stethoscope.Log.Internal.Storage.Linq;
using Stethoscope.Reactive;
using Stethoscope.Reactive.Linq;

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Stethoscope.Tests
{
    [TestFixture]
    public class ObservableTests
    {
        private IList<int> mockList;
        private IBaseListCollection<int> mockBaseList;
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
            mockList = Substitute.For<IList<int>>();
            mockBaseList = Substitute.For<IBaseListCollection<int>>();
            mockRegistryStorage = Substitute.For<IRegistryStorage>();
            mockLogEntry = Substitute.For<ILogEntry>();
        }

        [Test]
        public void ToObservableListNull([Values]ObservableType type)
        {
            IList<int> list = null;

            Assert.Throws<ArgumentNullException>(() =>
            {
                list.ToObservable(type);
            });
        }

        [Test]
        public void ToObservableBaseListCollectionNull([Values]ObservableType type)
        {
            IBaseListCollection<int> list = null;

            Assert.Throws<ArgumentNullException>(() =>
            {
                list.ToObservable(type);
            });
        }

        [Test]
        public void ToObservableList([Values]ObservableType type)
        {
            var list = new List<int>();

            var obs = list.ToObservable(type);
            Assert.That(obs, Is.Not.Null);
        }

        [Test]
        public void ToObservableBaseListCollection([Values]ObservableType type)
        {
            var list = new List<int>();
            var baseList = list.AsListCollection();

            var obs = baseList.ToObservable(type);
            Assert.That(obs, Is.Not.Null);
        }

        [Test]
        public void GetObservableTypeNull()
        {
            IObservable<int> obs = null;

            Assert.Throws<ArgumentNullException>(() =>
            {
                obs.GetObservableType();
            });
        }

        [Test]
        public void GetObservableTypeList([Values]ObservableType type)
        {
            var list = new List<int>();
            var obs = list.ToObservable(type);

            var testedType = obs.GetObservableType();
            Assert.That(testedType, Is.EqualTo(type));
        }

        [Test]
        public void GetObservableTypeBaseListCollection([Values]ObservableType type)
        {
            var list = new List<int>();
            var baseList = list.AsListCollection();
            var obs = baseList.ToObservable(type);

            var testedType = obs.GetObservableType();
            Assert.That(testedType, Is.EqualTo(type));
        }

        [Test]
        public void SchedulerListNull()
        {
            var list = new List<int>();

            Assert.Throws<ArgumentNullException>(() =>
            {
                list.ToObservable(ObservableType.LiveUpdating, null);
            });
        }

        [Test]
        public void SchedulerBaseListCollectionNull()
        {
            var list = new List<int>();
            var baseList = list.AsListCollection();

            Assert.Throws<ArgumentNullException>(() =>
            {
                baseList.ToObservable(ObservableType.LiveUpdating, null);
            });
        }
        
        [Test]
        public void SchedulerConfidenceCheck()
        {
            bool hasRegularScheduler = false;
            bool hasLongScheduler = false;

            foreach (var sched in SchedulersToTest)
            {
                if (sched == null)
                {
                    Assert.Fail("Schedulers shouldn't be null");
                }
                else
                {
                    hasRegularScheduler = true;
                }
                if (sched is ISchedulerLongRunning)
                {
                    var mod = sched.AsLongRunning();
                    if (mod != null)
                    {
                        hasLongScheduler = true;
                    }
                }
            }

            Assert.That(hasRegularScheduler, Is.True);
            Assert.That(hasLongScheduler, Is.True);
        }
        
        [Test]
        public void LiveObservableListException([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            mockList.Count.Returns(c => throw new InvalidOperationException());

            var obs = mockList.ToObservable(ObservableType.LiveUpdating, scheduler);
            Assert.Throws<InvalidOperationException>(() =>
            {
                obs.Wait();
            });
        }

        [Test]
        public void LiveObservableBaseListCollectionException([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            mockBaseList.Count.Returns(c => throw new InvalidOperationException());

            var obs = mockBaseList.ToObservable(ObservableType.LiveUpdating, scheduler);
            Assert.Throws<InvalidOperationException>(() =>
            {
                obs.Wait();
            });
        }

        [Test]
        public void LiveObservableListEmpty([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>();

            var obs = list.ToObservable(ObservableType.LiveUpdating, scheduler);
            Assert.That(obs.IsEmpty().Wait());
        }

        [Test]
        public void LiveObservableBaseListCollectionEmpty([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>();
            var baseList = list.AsListCollection();

            var obs = baseList.ToObservable(ObservableType.LiveUpdating, scheduler);
            Assert.That(obs.IsEmpty().Wait());
        }

        [Test]
        public void LiveObservableListToEnumerable([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>
            {
                10,
                20
            };

            var obs = list.ToObservable(ObservableType.LiveUpdating, scheduler);
            Assert.That(obs.ToEnumerable(), Is.EquivalentTo(new int[] { 10, 20 }));
        }

        [Test]
        public void LiveObservableBaseListCollectionToEnumerable([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>
            {
                10,
                20
            };
            var baseList = list.AsListCollection();

            var obs = baseList.ToObservable(ObservableType.LiveUpdating, scheduler);
            Assert.That(obs.ToEnumerable(), Is.EquivalentTo(new int[] { 10, 20 }));
        }
        
        [Test]
        public void LiveObservableList([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>
            {
                10,
                20
            };

            var recievedValues = new List<int>();
            bool recievedError = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var obs = list.ToObservable(ObservableType.LiveUpdating, scheduler);
            var dis = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => waitSem.Release());
            
            while (!waitSem.Wait(100)) ;
            
            dis.Dispose();

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
        }

        [Test]
        public void LiveObservableBaseListCollection([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>
            {
                10,
                20
            };
            var baseList = list.AsListCollection();

            var recievedValues = new List<int>();
            bool recievedError = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var obs = baseList.ToObservable(ObservableType.LiveUpdating, scheduler);
            var dis = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => waitSem.Release());

            while (!waitSem.Wait(100)) ;

            dis.Dispose();

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
        }

        [Test]
        public void LiveObservableListInsert([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>
            {
                10,
                20
            };

            var obs = list.ToObservable(ObservableType.LiveUpdating, scheduler);
            Assert.That(obs.ToEnumerable(), Is.EquivalentTo(new int[] { 10, 20 }));

            list.Insert(1, 30);
            Assert.That(obs.ToEnumerable(), Is.EquivalentTo(new int[] { 10, 30, 20 }));
        }

        [Test]
        public void LiveObservableBaseListCollectionInsert([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>
            {
                10,
                20
            };
            var baseList = list.AsListCollection();

            var obs = baseList.ToObservable(ObservableType.LiveUpdating, scheduler);
            Assert.That(obs.ToEnumerable(), Is.EquivalentTo(new int[] { 10, 20 }));

            baseList.Insert(1, 30);
            Assert.That(obs.ToEnumerable(), Is.EquivalentTo(new int[] { 10, 30, 20 }));
        }

        [Test, Repeat(3)]
        public void LiveObservableListInsertThreaded([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            /* Test parts:
             * 1. observe values when insertion and observation occurs (at least from an API call perspective) at the same time
             * 2. observe values when insertion occurs after observation was started
             */

            var list = new List<int>
            {
                10,
                20
            };

            var obs = list.ToObservable(ObservableType.LiveUpdating, scheduler);

            var recievedValues = new List<int>();
            bool recievedError = false;

            void listInsert() => list.Insert(1, 30);
            void enumerateValues()
            {
                var waitSem = new System.Threading.SemaphoreSlim(0);

                var dis = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => waitSem.Release());

                while (!waitSem.Wait(100)) ;

                dis.Dispose();
            }

            // 1

            Parallel.Invoke
            (
                listInsert,
                enumerateValues
            );

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 30, 20 }));
            Assert.That(recievedError, Is.False);

            // 2

            recievedValues.Clear();
            recievedError = false;

            list.RemoveAt(1);

            var enumerateTask = Task.Run((Action)enumerateValues);
            var insertTask = Task.Delay(1).ContinueWith(_ => listInsert());

            Task.WaitAll(enumerateTask, insertTask);

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
        }

        [Test, Repeat(3)]
        public void LiveObservableBaseListCollectionInsertThreaded([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            /* Test parts:
             * 1. observe values when insertion and observation occurs (at least from an API call perspective) at the same time
             * 2. observe values when insertion occurs after observation was started
             */

            var list = new List<int>
            {
                10,
                20
            };
            var baseList = list.AsListCollection();

            var obs = baseList.ToObservable(ObservableType.LiveUpdating, scheduler);

            var recievedValues = new List<int>();
            bool recievedError = false;

            void listInsert() => baseList.Insert(1, 30);
            void enumerateValues()
            {
                var waitSem = new System.Threading.SemaphoreSlim(0);

                var dis = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => waitSem.Release());

                while (!waitSem.Wait(100)) ;

                dis.Dispose();
            }

            // 1

            Parallel.Invoke
            (
                listInsert,
                enumerateValues
            );

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 30, 20 }));
            Assert.That(recievedError, Is.False);

            // 2

            recievedValues.Clear();
            recievedError = false;

            list.RemoveAt(1);

            var enumerateTask = Task.Run((Action)enumerateValues);
            var insertTask = Task.Delay(1).ContinueWith(_ => listInsert());

            Task.WaitAll(enumerateTask, insertTask);

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
        }

        [Test]
        public void InfiniteLiveObservableListException([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            mockList.Count.Returns(c => throw new InvalidOperationException());

            var obs = mockList.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);
            Assert.Throws<InvalidOperationException>(() =>
            {
                obs.Wait();
            });
        }

        [Test]
        public void InfiniteLiveObservableBaseListCollectionException([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            mockBaseList.Count.Returns(c => throw new InvalidOperationException());

            var obs = mockBaseList.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);
            Assert.Throws<InvalidOperationException>(() =>
            {
                obs.Wait();
            });
        }

        [Test]
        public void InfiniteLiveObservableListEmpty([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>();

            bool recievedValue = false;
            bool recievedError = false;
            bool completed = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var obs = list.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);
            var dis1 = obs.Subscribe(_ => recievedValue = true, _ => recievedError = true, () => completed = true);

            var dis2 = scheduler.Schedule(() =>
            {
                System.Threading.Thread.Sleep(50);
                waitSem.Release();
            });

            while (!waitSem.Wait(100)) ;

            dis1.Dispose();
            dis2.Dispose();

            Assert.That(recievedValue, Is.False);
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);
        }

        [Test]
        public void InfiniteLiveObservableBaseListCollectionEmpty([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>();
            var baseList = list.AsListCollection();

            bool recievedValue = false;
            bool recievedError = false;
            bool completed = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var obs = baseList.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);
            var dis1 = obs.Subscribe(_ => recievedValue = true, _ => recievedError = true, () => completed = true);

            var dis2 = scheduler.Schedule(() =>
            {
                System.Threading.Thread.Sleep(50);
                waitSem.Release();
            });

            while (!waitSem.Wait(100)) ;

            dis1.Dispose();
            dis2.Dispose();

            Assert.That(recievedValue, Is.False);
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);
        }

        [Test]
        public void InfiniteLiveObservableList([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>
            {
                10,
                20
            };
            
            var recievedValues = new List<int>();
            bool recievedError = false;
            bool completed = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var obs = list.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);
            var dis1 = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => completed = true);

            var dis2 = scheduler.Schedule(() =>
            {
                if (recievedValues.Count != 2)
                {
                    System.Threading.Thread.Sleep(50);
                }
                waitSem.Release();
            });

            while (!waitSem.Wait(100)) ;

            dis1.Dispose();
            dis2.Dispose();
            
            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);
        }

        [Test]
        public void InfiniteLiveObservableBaseListCollection([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>
            {
                10,
                20
            };
            var baseList = list.AsListCollection();

            var recievedValues = new List<int>();
            bool recievedError = false;
            bool completed = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var obs = baseList.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);
            var dis1 = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => completed = true);

            var dis2 = scheduler.Schedule(() =>
            {
                if (recievedValues.Count != 2)
                {
                    System.Threading.Thread.Sleep(50);
                }
                waitSem.Release();
            });

            while (!waitSem.Wait(100)) ;

            dis1.Dispose();
            dis2.Dispose();

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);
        }

        [Test, Repeat(3)]
        public void InfiniteLiveObservableListInsert([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            /* Test parts:
             * 1. observe values (observable is still running)
             * 2. insert value (observable is terminated) and ensure it was not observed (since it would be before the current "iteration index")
             * 3. new observe values (observable is terminated) but this one should have the inserted value as it's a new iteration index and the value came after the new iteration index
             */

            var list = new List<int>
            {
                10,
                20
            };

            // 1

            var recievedValues = new List<int>();
            bool recievedError = false;
            bool completed = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var obs = list.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);
            var dis1 = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => completed = true);

            var dis2 = scheduler.Schedule(() =>
            {
                if (recievedValues.Count != 2)
                {
                    System.Threading.Thread.Sleep(50);
                }
                waitSem.Release();
            });

            while (!waitSem.Wait(100)) ;

            dis2.Dispose();

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);

            // 2

            list.Insert(1, 30);

            dis2 = scheduler.Schedule(() =>
            {
                if (recievedValues.Count != 2)
                {
                    System.Threading.Thread.Sleep(50);
                }
                waitSem.Release();
            });

            while (!waitSem.Wait(100)) ;

            dis1.Dispose();
            dis2.Dispose();

            // Difference between this and the finite one
            // - finite one will have finished and a new subscription is needed to get the inserted value
            // - infinite one will still be running and will get the notification of the inserted item. But the insertion will be past where the index is, so it will notify of it.
            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);

            // 3

            recievedValues.Clear();
            recievedError = false;
            completed = false;

            dis1 = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => completed = true);
            dis2 = scheduler.Schedule(() =>
            {
                if (recievedValues.Count != 3)
                {
                    System.Threading.Thread.Sleep(50);
                }
                waitSem.Release();
            });

            while (!waitSem.Wait(100)) ;

            dis1.Dispose();
            dis2.Dispose();

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 30, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);
        }

        [Test, Repeat(3)]
        public void InfiniteLiveObservableBaseListCollectionInsert([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            /* Test parts:
             * 1. observe values (observable is still running)
             * 2. insert value (observable is terminated) and ensure it was not observed (since it would be before the current "iteration index")
             * 3. new observe values (observable is terminated) but this one should have the inserted value as it's a new iteration index and the value came after the new iteration index
             */
            
            // 1

            var list = new List<int>
            {
                10,
                20
            };
            var baseList = list.AsListCollection();

            var recievedValues = new List<int>();
            bool recievedError = false;
            bool completed = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var obs = baseList.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);
            var dis1 = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => completed = true);

            var dis2 = scheduler.Schedule(() =>
            {
                if (recievedValues.Count != 2)
                {
                    System.Threading.Thread.Sleep(50);
                }
                waitSem.Release();
            });

            while (!waitSem.Wait(100)) ;

            dis2.Dispose();

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);

            // 2

            list.Insert(1, 30);

            dis2 = scheduler.Schedule(() =>
            {
                if (recievedValues.Count != 2)
                {
                    System.Threading.Thread.Sleep(50);
                }
                waitSem.Release();
            });

            while (!waitSem.Wait(100)) ;

            dis1.Dispose();
            dis2.Dispose();

            // Difference between this and the finite one
            // - finite one will have finished and a new subscription is needed to get the inserted value
            // - infinite one will still be running and will get the notification of the inserted item. But the insertion will be past where the index is, so it will notify of it.
            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);

            // 3

            recievedValues.Clear();
            recievedError = false;
            completed = false;

            dis1 = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => completed = true);
            dis2 = scheduler.Schedule(() =>
            {
                if (recievedValues.Count != 3)
                {
                    System.Threading.Thread.Sleep(50);
                }
                waitSem.Release();
            });

            while (!waitSem.Wait(100)) ;

            dis1.Dispose();
            dis2.Dispose();

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 30, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);
        }

        [Test, Repeat(3)]
        public void InfiniteLiveObservableListInsertThreaded([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            /* Test parts:
             * 1. observe values when insertion and observation occurs (at least from an API call perspective) at the same time
             * 2. observe values when insertion occurs after observation was started
             */

            var list = new List<int>
            {
                10,
                20
            };

            var obs = list.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);

            var recievedValues = new List<int>();
            bool recievedError = false;
            bool completed = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            void listInsert() => list.Insert(1, 30);
            Action enumerateValues(int expectedCount) => new Action(() =>
            {
                var dis1 = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => completed = true);
                var dis2 = scheduler.Schedule(() =>
                {
                    if (recievedValues.Count != expectedCount)
                    {
                        System.Threading.Thread.Sleep(50);
                    }
                    waitSem.Release();
                });

                while (!waitSem.Wait(100)) ;

                dis1.Dispose();
                dis2.Dispose();
            });

            // 1

            Parallel.Invoke
            (
                listInsert,
                enumerateValues(3)
            );

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 30, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);

            // 2

            recievedValues.Clear();
            recievedError = false;
            completed = false;

            list.RemoveAt(1);

            var enumerateTask = Task.Run(enumerateValues(2));
            var insertTask = Task.Delay(1).ContinueWith(_ => listInsert());

            Task.WaitAll(enumerateTask, insertTask);

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);
        }

        [Test, Repeat(3)]
        public void InfiniteLiveObservableBaseListCollectionInsertThreaded([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            /* Test parts:
             * 1. observe values when insertion and observation occurs (at least from an API call perspective) at the same time
             * 2. observe values when insertion occurs after observation was started
             */

            var list = new List<int>
            {
                10,
                20
            };
            var baseList = list.AsListCollection();

            var obs = baseList.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);

            var recievedValues = new List<int>();
            bool recievedError = false;
            bool completed = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            void listInsert() => baseList.Insert(1, 30);
            Action enumerateValues(int expectedCount) => new Action(() =>
            {
                var dis1 = obs.Subscribe(x => recievedValues.Add(x), _ => recievedError = true, () => completed = true);
                var dis2 = scheduler.Schedule(() =>
                {
                    if (recievedValues.Count != expectedCount)
                    {
                        System.Threading.Thread.Sleep(50);
                    }
                    waitSem.Release();
                });

                while (!waitSem.Wait(100)) ;

                dis1.Dispose();
                dis2.Dispose();
            });

            // 1

            Parallel.Invoke
            (
                listInsert,
                enumerateValues(3)
            );

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 30, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);

            // 2

            recievedValues.Clear();
            recievedError = false;
            completed = false;

            list.RemoveAt(1);

            var enumerateTask = Task.Run(enumerateValues(2));
            var insertTask = Task.Delay(1).ContinueWith(_ => listInsert());

            Task.WaitAll(enumerateTask, insertTask);

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10, 20 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);
        }

        [Test, Repeat(3)]
        public void InfiniteLiveObservableListPostAddition([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>();

            var obs = list.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);

            var recievedValues = new List<int>();
            bool recievedError = false;
            bool completed = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var addedTime = DateTime.Now.AddSeconds(1);

            void onValue(int x)
            {
                addedTime = DateTime.Now;
                recievedValues.Add(x);
                waitSem.Release();
            }

            var dis = obs.Subscribe(onValue, _ => recievedError = true, () => completed = true);

            var insertTime = DateTime.Now;
            list.Add(10);

            while (!waitSem.Wait(10)) ;

            dis.Dispose();
            
            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);
            Assert.That(addedTime, Is.EqualTo(insertTime).Within(125).Milliseconds);
        }

        [Test, Repeat(3)]
        public void InfiniteLiveObservableBaseListCollectionPostAddition([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            var list = new List<int>();
            var baseList = list.AsListCollection();

            var obs = baseList.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);

            var recievedValues = new List<int>();
            bool recievedError = false;
            bool completed = false;
            var waitSem = new System.Threading.SemaphoreSlim(0);

            var addedTime = DateTime.Now.AddSeconds(1);

            void onValue(int x)
            {
                addedTime = DateTime.Now;
                recievedValues.Add(x);
                waitSem.Release();
            }

            var dis = obs.Subscribe(onValue, _ => recievedError = true, () => completed = true);

            var insertTime = DateTime.Now;
            baseList.Add(10);

            while (!waitSem.Wait(10)) ;

            dis.Dispose();

            Assert.That(recievedValues, Is.EquivalentTo(new int[] { 10 }));
            Assert.That(recievedError, Is.False);
            Assert.That(completed, Is.False);
            Assert.That(addedTime, Is.EqualTo(insertTime).Within(125).Milliseconds);
        }

        // Want to test a long scheduler that uses CancellationDisposable, but no current implementations utilizes it. Instead they all utilize BooleanDisposable which has no way to determine if it's been disposed without polling.
    }
}
