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

        //TODO: redo LogRegistry tests so any tests that are really testing ListStorage are moved here

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
}
