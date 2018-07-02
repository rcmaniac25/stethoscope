using NSubstitute;

using NUnit.Framework;

using Stethoscope.Common;
using Stethoscope.Log.Internal;
using Stethoscope.Log.Internal.Storage;
using Stethoscope.Tests.Helpers;

using System;

namespace Stethoscope.Tests
{
    [TestFixture]
    public class RegistryStorageTests
    {
        private ILogEntry logEntry;

        [SetUp]
        public void Setup()
        {
            logEntry = Substitute.For<IInternalLogEntry>();

            SetupLogEntry(logEntry, true);
        }

        private static void SetupLogEntry(ILogEntry entry, bool isValid)
        {
            if (entry is IInternalLogEntry internalEntry)
            {
                internalEntry.ID.Returns(Guid.NewGuid());
            }
            entry.IsValid.Returns(isValid);
        }

        [Test(TestOf = typeof(ListStorage))]
        public void ListStorageConstructorSetsSortAttribute()
        {
            var storage = new ListStorage();
            Assert.That(storage.SortAttribute, Is.EqualTo(LogAttribute.Timestamp));
        }

        [Test(TestOf = typeof(ListStorage))]
        public void ListStorageEmptyEntriesDefault()
        {
            var storage = new ListStorage();

            Assert.That(storage.Count, Is.Zero);
            Assert.That(storage.Entries, IsEx.ExEmpty);
        }

        [Test(TestOf = typeof(ListStorage))]
        public void ListStorageAddLogSorted()
        {
            var storage = new ListStorage();
            Assert.That(storage.Count, Is.Zero);

            // Technically, various values should be set... but unless we throw an error, it should still work
            storage.AddLogSorted(logEntry);

            Assert.That(storage.Count, Is.EqualTo(1));
            Assert.That(storage.Entries, Is.Not.ExEmpty());
        }

        [Test(TestOf = typeof(ListStorage))]
        public void ListStorageAddLogSortedUnsupported()
        {
            var storage = new ListStorage();
            Assert.That(storage.Count, Is.Zero);

            // May seem weird, but for now we don't support arbitary ILogEntry
            var unsupportedLogEntry = Substitute.For<ILogEntry>();
            Assert.Throws<ArgumentException>(() =>
            {
                storage.AddLogSorted(unsupportedLogEntry);
            });
        }

        [Test(TestOf = typeof(ListStorage))]
        public void ListStorageClear()
        {
            var storage = new ListStorage();
            Assert.That(storage.Count, Is.Zero);

            // Technically, various values should be set... but unless we throw an error, it should still work
            storage.AddLogSorted(logEntry);

            Assert.That(storage.Count, Is.EqualTo(1));
            Assert.That(storage.Entries, Is.Not.ExEmpty());

            storage.Clear();

            Assert.That(storage.Count, Is.Zero);
            Assert.That(storage.Entries, IsEx.ExEmpty);
        }

        [Test(TestOf = typeof(NullStorage))]
        public void NullStorageConstructorSetsSortAttribute()
        {
            var storage1 = new NullStorage(LogAttribute.Context);
            Assert.That(storage1.SortAttribute, Is.EqualTo(LogAttribute.Context));

            var storage2 = new NullStorage(LogAttribute.Timestamp);
            Assert.That(storage2.SortAttribute, Is.EqualTo(LogAttribute.Timestamp));
        }

        [Test(TestOf = typeof(NullStorage))]
        public void NullStorageEmptyEntriesDefault()
        {
            var storage = new NullStorage(LogAttribute.Timestamp);

            Assert.That(storage.Count, Is.Zero);
            Assert.That(storage.Entries, IsEx.ExEmpty);
        }

        [Test(TestOf = typeof(NullStorage))]
        public void NullStorageEmptyEntries()
        {
            var storage = new NullStorage(LogAttribute.Timestamp);
            Assert.That(storage.Count, Is.Zero);

            storage.AddLogSorted(logEntry);

            Assert.That(storage.Count, Is.EqualTo(1));
            Assert.That(storage.Entries, IsEx.ExEmpty);
        }

        [Test(TestOf = typeof(NullStorage))]
        public void NullStorageClear()
        {
            var storage = new NullStorage(LogAttribute.Timestamp);
            Assert.That(storage.Count, Is.Zero);

            storage.AddLogSorted(logEntry);

            Assert.That(storage.Count, Is.EqualTo(1));

            storage.Clear();

            Assert.That(storage.Count, Is.Zero);
        }
    }

    //TODO: with the exception of NullStorage, all storage types should act the same. So maybe once other types are added, test that they all work the same.
}
