﻿using System;

namespace LogTracker.Printers.Internal
{
    public class ConsolePrinter : IOPrinter
    {
        public override void Setup()
        {
            TextWriter = Console.Out;
        }

        public override void Teardown()
        {
        }
    }
}
