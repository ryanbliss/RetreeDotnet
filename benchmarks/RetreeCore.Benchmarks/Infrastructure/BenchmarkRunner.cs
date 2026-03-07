// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using RetreeCore.Internal;

namespace RetreeCore.Benchmarks.Infrastructure
{
    public static class BenchmarkRunner
    {
        public static List<BenchmarkResult> RunAll(IEnumerable<BenchmarkScenario> scenarios, bool verbose = false)
        {
            var results = new List<BenchmarkResult>();
            foreach (var scenario in scenarios)
            {
                if (verbose)
                    Console.WriteLine($"  Running {scenario.Name}...");

                var result = RunSingle(scenario);
                results.Add(result);

                if (verbose)
                    Console.WriteLine($"  Done: {result.TotalChangesDetected} changes, tick mean {result.TickStats.Mean:F4} ms");
            }
            return results;
        }

        public static BenchmarkResult RunSingle(BenchmarkScenario scenario)
        {
            var result = new BenchmarkResult(
                scenario.Name,
                scenario.TreeSize,
                scenario.OpCategory,
                scenario.MeasuredIterations
            );

            var tracker = new LatencyTracker();
            var tickTimer = new BenchmarkTimer();
            var totalTimer = new BenchmarkTimer();

            // --- Warmup ---
            for (int w = 0; w < scenario.WarmupIterations; w++)
            {
                RunIteration(scenario, tracker, tickTimer, totalTimer, discard: true);
            }

            // --- Measured iterations ---
            for (int i = 0; i < scenario.MeasuredIterations; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                RunIteration(scenario, tracker, tickTimer, totalTimer, discard: false);

                result.TickDurationsMs[i] = tickTimer.ElapsedMs;
                result.TotalTimesMs[i] = totalTimer.ElapsedMs;

                var iterationRecords = tracker.DrainRecords();
                result.ChangesPerIteration[i] = iterationRecords.Count;
                result.AllChangeRecords.AddRange(iterationRecords);
            }

            return result;
        }

        private static void RunIteration(
            BenchmarkScenario scenario,
            LatencyTracker tracker,
            BenchmarkTimer tickTimer,
            BenchmarkTimer totalTimer,
            bool discard)
        {
            Retree.Reset();

            var root = scenario.TreeFactory();
            var ops = scenario.OpFactory(root);

            tracker.Clear();
            tracker.Attach(root);

            // Initial tick to set up snapshots for all nodes that now have listeners
            Retree.Tick();
            tracker.Clear(); // discard setup emissions

            totalTimer.Start();

            // Execute all mutations
            foreach (var op in ops)
            {
                op();
            }

            // Tick to detect deferred field changes
            tracker.OnTickStart();
            tickTimer.Start();
            Retree.Tick();
            tickTimer.Stop();

            totalTimer.Stop();

            tracker.Detach();

            if (discard)
                tracker.Clear();
        }

        public static int CountNodes(RetreeBase root)
        {
            int count = 1;
            if (root is RetreeNode node)
            {
                var fields = FieldCache.GetFields(node.GetType());
                foreach (var field in fields)
                {
                    if (field.Kind == FieldKind.Value) continue;
                    var child = field.Getter(node) as RetreeBase;
                    if (child != null)
                        count += CountNodes(child);
                }
            }
            else if (root is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is RetreeBase childBase)
                        count += CountNodes(childBase);
                }
            }
            return count;
        }
    }
}
