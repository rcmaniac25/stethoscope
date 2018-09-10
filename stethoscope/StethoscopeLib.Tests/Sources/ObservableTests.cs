using NSubstitute;

using NUnit.Framework;

using Stethoscope.Collections;
using Stethoscope.Reactive;

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Stethoscope.Tests
{
    [TestFixture]
    public class ObservableTests
    {
        private IList<int> mockList;
        private IBaseListCollection<int> mockBaseList;

        private static IScheduler[] SchedulersToTest = new IScheduler[]
        {
            DefaultScheduler.Instance, // Regular scheduler
            TaskPoolScheduler.Default  // Long scheduler
        };

        [SetUp]
        public void Setup()
        {
            mockList = Substitute.For<IList<int>>();
            mockBaseList = Substitute.For<IBaseListCollection<int>>();
        }

        [Test]
        public void ToObservableListNull([Values]ObservableType type)
        {
            IList<int> list = null;

            Assert.Throws<System.ArgumentNullException>(() =>
            {
                list.ToObservable(type);
            });
        }

        [Test]
        public void ToObservableBaseListCollectionNull([Values]ObservableType type)
        {
            IBaseListCollection<int> list = null;

            Assert.Throws<System.ArgumentNullException>(() =>
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
            System.IObservable<int> obs = null;

            Assert.Throws<System.ArgumentNullException>(() =>
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

            Assert.Throws<System.ArgumentNullException>(() =>
            {
                list.ToObservable(ObservableType.LiveUpdating, null);
            });
        }

        [Test]
        public void SchedulerBaseListCollectionNull()
        {
            var list = new List<int>();
            var baseList = list.AsListCollection();

            Assert.Throws<System.ArgumentNullException>(() =>
            {
                baseList.ToObservable(ObservableType.LiveUpdating, null);
            });
        }
        
        [Test]
        public void SchedulerSanityCheck()
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
            mockList.Count.Returns(c => throw new System.InvalidOperationException());

            var obs = mockList.ToObservable(ObservableType.LiveUpdating, scheduler);
            Assert.Throws<System.InvalidOperationException>(() =>
            {
                obs.Wait();
            });
        }

        [Test]
        public void LiveObservableBaseListCollectionException([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            mockBaseList.Count.Returns(c => throw new System.InvalidOperationException());

            var obs = mockBaseList.ToObservable(ObservableType.LiveUpdating, scheduler);
            Assert.Throws<System.InvalidOperationException>(() =>
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
        public void LiveObservableList([ValueSource("SchedulersToTest")]IScheduler scheduler)
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
        public void LiveObservableBaseListCollection([ValueSource("SchedulersToTest")]IScheduler scheduler)
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
        
        [Test]
        public void InfiniteLiveObservableListException([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            mockList.Count.Returns(c => throw new System.InvalidOperationException());

            var obs = mockList.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);
            Assert.Throws<System.InvalidOperationException>(() =>
            {
                obs.Wait();
            });
        }

        [Test]
        public void InfiniteLiveObservableBaseListCollectionException([ValueSource("SchedulersToTest")]IScheduler scheduler)
        {
            mockBaseList.Count.Returns(c => throw new System.InvalidOperationException());

            var obs = mockBaseList.ToObservable(ObservableType.InfiniteLiveUpdating, scheduler);
            Assert.Throws<System.InvalidOperationException>(() =>
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

        //TODO: insert into list (non-threaded)

        //TODO: insert into list (threaded)

        //TODO: something that produces a CancellationDisposable for use cancelable

        //TODO: some long test
    }
}
