// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Retree.Benchmarks.Infrastructure
{
    public class BenchmarkTimer
    {
        private long _startTicks;
        private long _elapsedTicks;

        public double ElapsedMs => (double)_elapsedTicks / Stopwatch.Frequency * 1000.0;
        public long ElapsedRawTicks => _elapsedTicks;

        public void Start()
        {
            _startTicks = Stopwatch.GetTimestamp();
        }

        public void Stop()
        {
            _elapsedTicks = Stopwatch.GetTimestamp() - _startTicks;
        }

        public static BenchmarkTimer Time(Action action)
        {
            var timer = new BenchmarkTimer();
            timer.Start();
            action();
            timer.Stop();
            return timer;
        }
    }
}
