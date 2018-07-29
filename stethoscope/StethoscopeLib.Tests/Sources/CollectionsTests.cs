using NUnit.Framework;

using Stethoscope.Collections;

using System;
using System.Collections.Generic;
using System.Text;

namespace Stethoscope.Tests
{
    [TestFixture]
    public class CollectionsTests
    {
        [Test(TestOf = typeof(IBaseListCollection<>))]
        public void EmptyCollection()
        {
            var list = new List<int>();
            Assert.That(list, Is.Empty);

            var collection = list.AsListCollection();
            Assert.That(collection, Is.Empty);
            Assert.That(collection.Count, Is.Zero);
        }

        [Test(TestOf = typeof(IBaseListCollection<>))]
        public void ReadWriteCollection()
        {
            var list = new List<int>();
            Assert.That(list, Is.Empty);

            var collection = list.AsListCollection();
            Assert.That(collection, Is.Empty);
            Assert.That(collection.Count, Is.Zero);
            Assert.That(collection.IsReadOnly, Is.False);

            collection.Add(10);

            Assert.That(list, Is.Not.Empty);

            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));
        }

        [Test(TestOf = typeof(IBaseListCollection<>))]
        public void ReadOnlyCollection()
        {
            var list = new List<int>();
            Assert.That(list, Is.Empty);

            var collection = ((IReadOnlyList<int>)list).AsListCollection();
            Assert.That(collection, Is.Empty);
            Assert.That(collection.Count, Is.Zero);
            Assert.That(collection.IsReadOnly, Is.True);
        }

        //TODO: GetAt (valid and invalid cases)

        //TODO: binary search functions

        //TODO: index get (read-only)

        //TODO: index get/set (read-write)

        //TODO: add (read-write)

        //TODO: insert (read-write)

        //TODO: clear (read-write)

        //TODO: events
    }
}
