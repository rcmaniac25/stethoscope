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

        //TODO: ToObservables

        [Test]
        public void GetObservableTypeNull()
        {
            System.IObservable<int> obs = null;
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                obs.GetObservableType();
            });
        }

        //TODO: GetObservableType

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
