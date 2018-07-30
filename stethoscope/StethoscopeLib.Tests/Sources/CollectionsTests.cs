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

        [Test(TestOf = typeof(IBaseListCollection<>))]
        public void GetAt()
        {
            var list = new List<int>()
            {
                10
            };

            var collection = list.AsListCollection();
            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));

            Assert.That(collection.GetAt(0), Is.EqualTo(10));
        }
        
        [TestCase(-1, TestOf = typeof(IBaseListCollection<>))]
        [TestCase(1, TestOf = typeof(IBaseListCollection<>))]
        public void GetAtOutOfBounds(int index)
        {
            var list = new List<int>()
            {
                10
            };

            var collection = list.AsListCollection();
            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                collection.GetAt(index);
            });
        }

        private class BinarySearchComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return x.CompareTo(y);
            }
        }

        // See the return value for https://msdn.microsoft.com/en-us/library/w4e7fxsh(v=vs.110).aspx to understand expected result
        // Count = 3
        [TestCase(12, ExpectedResult = 1, TestOf = typeof(IBaseListCollection<>))]
        [TestCase(57, ExpectedResult = 3, TestOf = typeof(IBaseListCollection<>))]
        [TestCase(10, ExpectedResult = ~1, TestOf = typeof(IBaseListCollection<>))] // [1] = 12 >= 10, so return bitwise complement of 1
        [TestCase(92, ExpectedResult = ~4, TestOf = typeof(IBaseListCollection<>))] // [4] = 92 >= 92, so return bitwise complement of 4
        [TestCase(13, ExpectedResult = ~2, TestOf = typeof(IBaseListCollection<>))] // [2] = 56 >= 13, so return bitwise complement of 2
        [TestCase(14, ExpectedResult = ~2, TestOf = typeof(IBaseListCollection<>))] // [2] = 56 >= 14, so return bitwise complement of 2
        [TestCase(112, ExpectedResult = ~4, TestOf = typeof(IBaseListCollection<>))] // Bitwise complement of Count (or so says the docs... reality it's just till the end of the search area)
        public int BinarySearchFull(int item)
        {
            var list = new List<int>()
            {
                10,
                12,
                56,
                57,
                92
            };

            var collection = list.AsListCollection();
            return collection.BinarySearch(1, 3, item, new BinarySearchComparer());
        }

        [TestCase(12, ExpectedResult = 1, TestOf = typeof(IBaseListCollection<>))]
        [TestCase(57, ExpectedResult = 3, TestOf = typeof(IBaseListCollection<>))]
        [TestCase(10, ExpectedResult = 0, TestOf = typeof(IBaseListCollection<>))]
        [TestCase(92, ExpectedResult = 4, TestOf = typeof(IBaseListCollection<>))]
        [TestCase(13, ExpectedResult = ~2, TestOf = typeof(IBaseListCollection<>))] // [2] = 56 >= 13, so return bitwise complement of 2
        [TestCase(14, ExpectedResult = ~2, TestOf = typeof(IBaseListCollection<>))] // [2] = 56 >= 14, so return bitwise complement of 2
        [TestCase(112, ExpectedResult = ~5, TestOf = typeof(IBaseListCollection<>))] // Bitwise complement of Count
        public int BinarySearchExtComparer(int item)
        {
            var list = new List<int>()
            {
                10,
                12,
                56,
                57,
                92
            };

            var collection = list.AsListCollection();
            return collection.BinarySearch(item, new BinarySearchComparer());
        }

        [TestCase(12, ExpectedResult = 1, TestOf = typeof(IBaseListCollection<>))]
        [TestCase(57, ExpectedResult = 3, TestOf = typeof(IBaseListCollection<>))]
        [TestCase(10, ExpectedResult = 0, TestOf = typeof(IBaseListCollection<>))]
        [TestCase(92, ExpectedResult = 4, TestOf = typeof(IBaseListCollection<>))]
        [TestCase(13, ExpectedResult = ~2, TestOf = typeof(IBaseListCollection<>))] // [2] = 56 >= 13, so return bitwise complement of 2
        [TestCase(14, ExpectedResult = ~2, TestOf = typeof(IBaseListCollection<>))] // [2] = 56 >= 14, so return bitwise complement of 2
        [TestCase(112, ExpectedResult = ~5, TestOf = typeof(IBaseListCollection<>))] // Bitwise complement of Count
        public int BinarySearchExt(int item)
        {
            var list = new List<int>()
            {
                10,
                12,
                56,
                57,
                92
            };

            var collection = list.AsListCollection();
            return collection.BinarySearch(item);
        }

        [Test(TestOf = typeof(IBaseReadOnlyListCollection<>))]
        public void IndexAccessorReadOnly()
        {
            var list = new List<int>()
            {
                10
            };

            var collection = ((IReadOnlyList<int>)list).AsListCollection();
            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));

            Assert.That(collection[0], Is.EqualTo(10));
        }

        [TestCase(-1, TestOf = typeof(IBaseReadOnlyListCollection<>))]
        [TestCase(1, TestOf = typeof(IBaseReadOnlyListCollection<>))]
        public void IndexAccessorReadOnlyOutOfBounds(int index)
        {
            var list = new List<int>()
            {
                10
            };

            var collection = ((IReadOnlyList<int>)list).AsListCollection();
            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var value = collection[index];
            });
        }

        [Test(TestOf = typeof(IBaseReadWriteListCollection<>))]
        public void IndexAccessorReadWrite()
        {
            var list = new List<int>()
            {
                10
            };

            var collection = list.AsListCollection();
            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));

            Assert.That(collection[0], Is.EqualTo(10));
        }

        [TestCase(-1, TestOf = typeof(IBaseReadWriteListCollection<>))]
        [TestCase(1, TestOf = typeof(IBaseReadWriteListCollection<>))]
        public void IndexAccessorReadWriteOutOfBounds(int index)
        {
            var list = new List<int>()
            {
                10
            };

            var collection = list.AsListCollection();
            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var value = collection[index];
            });
        }

        [Test(TestOf = typeof(IBaseReadWriteListCollection<>))]
        public void IndexSetterReadWrite()
        {
            var list = new List<int>()
            {
                10
            };

            var collection = list.AsListCollection();
            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));

            Assert.That(collection[0], Is.EqualTo(10));

            collection[0] = 20;

            Assert.That(collection[0], Is.EqualTo(20));
        }

        [TestCase(-1, TestOf = typeof(IBaseReadWriteListCollection<>))]
        [TestCase(1, TestOf = typeof(IBaseReadWriteListCollection<>))]
        public void IndexSetterReadWriteOutOfBounds(int index)
        {
            var list = new List<int>()
            {
                10
            };

            var collection = list.AsListCollection();
            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                collection[index] = 20;
            });
        }

        [Test(TestOf = typeof(IBaseReadWriteListCollection<>))]
        public void Add()
        {
            var list = new List<int>();

            var collection = list.AsListCollection();
            Assert.That(collection, Is.Empty);
            Assert.That(collection.Count, Is.Zero);

            collection.Add(10);

            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));
            Assert.That(collection[0], Is.EqualTo(10));
        }

        [Test(TestOf = typeof(IBaseReadWriteListCollection<>))]
        public void Insert()
        {
            var list = new List<int>()
            {
                10
            };

            var collection = list.AsListCollection();
            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));
            Assert.That(collection[0], Is.EqualTo(10));

            collection.Insert(0, 20);

            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(2));
            Assert.That(collection[0], Is.EqualTo(20));
            Assert.That(collection[1], Is.EqualTo(10));
        }

        [Test(TestOf = typeof(IBaseReadWriteListCollection<>))]
        public void Clear()
        {
            var list = new List<int>()
            {
                10
            };

            var collection = list.AsListCollection();
            Assert.That(collection, Is.Not.Empty);
            Assert.That(collection.Count, Is.EqualTo(1));
            Assert.That(collection[0], Is.EqualTo(10));

            collection.Clear();

            Assert.That(collection, Is.Empty);
            Assert.That(collection.Count, Is.Zero);
        }
        
        //TODO: events
    }
}
