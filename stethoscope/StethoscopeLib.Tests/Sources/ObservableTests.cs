using NSubstitute;

using NUnit.Framework;

using Stethoscope.Collections;
using Stethoscope.Reactive;

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

        //TODO: need a scheduler sanity-check to ensure all expected schedulers exist

        //=========All the following should accept an argument for a scheduler, and then a values schedule will be provided to test regular and long scheduler==========

        //--- Finite ---

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

        //TODO: no messages at all

        //TODO: a couple messages

        //TODO: insert into list (non-threaded)

        //TODO: insert into list (threaded)

        //--- Infinite ---

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

        //TODO: no messages at all

        //TODO: a couple messages

        //TODO: insert into list (non-threaded)

        //TODO: insert into list (threaded)

        //TODO: something that produces a CancellationDisposable for use cancelable
    }
}
