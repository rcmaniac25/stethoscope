using NSubstitute;

using NUnit.Framework;

using Stethoscope.Collections;
using Stethoscope.Reactive;

using System.Collections.Generic;

namespace Stethoscope.Tests
{
    [TestFixture]
    public class ObservableTests
    {
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

        //TODO: basic tests with regular schedulers and long running schedulers

        //--- Finite ---

        //TODO: exception

        //TODO: no messages at all

        //TODO: a couple messages

        //TODO: insert into list (non-threaded)

        //TODO: insert into list (threaded)

        //--- Infinite ---

        //TODO: exception

        //TODO: no messages at all

        //TODO: a couple messages

        //TODO: insert into list (non-threaded)

        //TODO: insert into list (threaded)

        //TODO: something that produces a CancellationDisposable for use cancelable
    }
}
