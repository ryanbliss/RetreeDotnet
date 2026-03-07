// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace RetreeCore.Benchmarks.Infrastructure
{
    public readonly struct Stats
    {
        public double Min { get; }
        public double Max { get; }
        public double Mean { get; }
        public double Median { get; }
        public double P95 { get; }
        public double P99 { get; }

        public Stats(double min, double max, double mean, double median, double p95, double p99)
        {
            Min = min;
            Max = max;
            Mean = mean;
            Median = median;
            P95 = p95;
            P99 = p99;
        }

        public static Stats Compute(double[] samples)
        {
            if (samples == null || samples.Length == 0)
                return new Stats(0, 0, 0, 0, 0, 0);

            var sorted = new double[samples.Length];
            Array.Copy(samples, sorted, samples.Length);
            Array.Sort(sorted);

            double min = sorted[0];
            double max = sorted[sorted.Length - 1];
            double mean = 0;
            for (int i = 0; i < sorted.Length; i++)
                mean += sorted[i];
            mean /= sorted.Length;

            double median = Percentile(sorted, 50);
            double p95 = Percentile(sorted, 95);
            double p99 = Percentile(sorted, 99);

            return new Stats(min, max, mean, median, p95, p99);
        }

        private static double Percentile(double[] sorted, double percentile)
        {
            double index = (percentile / 100.0) * (sorted.Length - 1);
            int lower = (int)Math.Floor(index);
            int upper = (int)Math.Ceiling(index);
            if (lower == upper || upper >= sorted.Length)
                return sorted[lower];
            double frac = index - lower;
            return sorted[lower] * (1.0 - frac) + sorted[upper] * frac;
        }
    }

    public class BenchmarkResult
    {
        public string ScenarioName { get; }
        public string TreeSize { get; }
        public string OpCategory { get; }
        public int NodeCount { get; set; }
        public int OperationCount { get; set; }
        public int Iterations { get; }

        public double[] TickDurationsMs { get; }
        public double[] TotalTimesMs { get; }
        public List<ChangeRecord> AllChangeRecords { get; }
        public int[] ChangesPerIteration { get; }

        public Stats TickStats => Stats.Compute(TickDurationsMs);
        public Stats TotalStats => Stats.Compute(TotalTimesMs);

        public Stats PropagationDepthStats
        {
            get
            {
                if (AllChangeRecords.Count == 0) return new Stats(0, 0, 0, 0, 0, 0);
                var depths = new double[AllChangeRecords.Count];
                for (int i = 0; i < AllChangeRecords.Count; i++)
                    depths[i] = AllChangeRecords[i].PropagationDepth;
                return Stats.Compute(depths);
            }
        }

        public double MeanChangesPerTick
        {
            get
            {
                if (ChangesPerIteration.Length == 0) return 0;
                double sum = 0;
                for (int i = 0; i < ChangesPerIteration.Length; i++)
                    sum += ChangesPerIteration[i];
                return sum / ChangesPerIteration.Length;
            }
        }

        public int TotalChangesDetected => AllChangeRecords.Count;

        public BenchmarkResult(string scenarioName, string treeSize, string opCategory, int iterations)
        {
            ScenarioName = scenarioName;
            TreeSize = treeSize;
            OpCategory = opCategory;
            Iterations = iterations;
            TickDurationsMs = new double[iterations];
            TotalTimesMs = new double[iterations];
            ChangesPerIteration = new int[iterations];
            AllChangeRecords = new List<ChangeRecord>();
        }

        public Dictionary<string, int> ChangesByOperationType()
        {
            var dict = new Dictionary<string, int>();
            foreach (var record in AllChangeRecords)
            {
                var key = record.OperationType ?? "Unknown";
                if (dict.ContainsKey(key))
                    dict[key]++;
                else
                    dict[key] = 1;
            }
            return dict;
        }

        public Dictionary<string, double> MeanDepthByOperationType()
        {
            var groups = new Dictionary<string, List<int>>();
            foreach (var record in AllChangeRecords)
            {
                var key = record.OperationType ?? "Unknown";
                if (!groups.ContainsKey(key))
                    groups[key] = new List<int>();
                groups[key].Add(record.PropagationDepth);
            }

            var result = new Dictionary<string, double>();
            foreach (var kvp in groups)
            {
                double sum = 0;
                for (int i = 0; i < kvp.Value.Count; i++)
                    sum += kvp.Value[i];
                result[kvp.Key] = sum / kvp.Value.Count;
            }
            return result;
        }
    }
}
