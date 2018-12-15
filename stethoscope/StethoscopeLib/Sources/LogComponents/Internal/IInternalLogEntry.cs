using Stethoscope.Common;

using System;

namespace Stethoscope.Log.Internal
{
    interface IInternalLogEntry : ILogEntry
    {
        Guid ID { get; }
        bool HasTimestampChanged { get; }
        bool IsEmpty { get; }

        void AddAttribute(LogAttribute attribute, object value);

        void ResetTimestampChanged();
    }
}
