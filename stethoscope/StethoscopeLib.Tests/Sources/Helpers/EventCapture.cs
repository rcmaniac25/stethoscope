using System;
using System.Collections.Generic;

namespace Stethoscope.Tests.Helpers
{
    public class EventCapture<T> where T : EventArgs
    {
        public List<(object sender, T eventObject)> CapturedEvents { get; private set; } = new List<(object sender, T eventObject)>();

        public void CaptureEventHandler(object sender, T e)
        {
            CapturedEvents.Add((sender, e));
        }
    }
}
