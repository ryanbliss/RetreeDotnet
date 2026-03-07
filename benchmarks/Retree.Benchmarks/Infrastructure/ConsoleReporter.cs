// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Retree.Benchmarks.Infrastructure
{
    public static class ConsoleReporter
    {
        private const string Separator = "================================================================================";
        private const string ThinSep   = "--------------------------------------------------------------------------------";

        public static void PrintHeader(int iterations, int warmup)
        {
            Console.WriteLine();
            Console.WriteLine(Separator);
            Console.WriteLine("  RETREE BENCHMARK RESULTS");
            Console.WriteLine($"  Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}  |  Iterations: {iterations}  |  Warmup: {warmup}  |  Runtime: net9.0");
            Console.WriteLine(Separator);
        }

        public static void PrintSummaryTable(List<BenchmarkResult> results)
        {
            Console.WriteLine();
            Console.WriteLine("  TICK DURATION (ms)");
            PrintTable(
                new[] { "Scenario", "Nodes", "Ops", "Min", "Mean", "Median", "P95", "P99", "Max" },
                new[] { 11, 7, 7, 8, 8, 8, 8, 8, 8 },
                results.Select(r => new[]
                {
                    r.ScenarioName,
                    r.NodeCount.ToString(),
                    r.OperationCount.ToString(),
                    r.TickStats.Min.ToString("F4"),
                    r.TickStats.Mean.ToString("F4"),
                    r.TickStats.Median.ToString("F4"),
                    r.TickStats.P95.ToString("F4"),
                    r.TickStats.P99.ToString("F4"),
                    r.TickStats.Max.ToString("F4")
                }).ToList()
            );

            Console.WriteLine();
            Console.WriteLine("  TOTAL SCENARIO TIME (ms) [mutations + tick + emission]");
            PrintTable(
                new[] { "Scenario", "Min", "Mean", "Median", "P95", "P99", "Max" },
                new[] { 11, 8, 8, 8, 8, 8, 8 },
                results.Select(r => new[]
                {
                    r.ScenarioName,
                    r.TotalStats.Min.ToString("F4"),
                    r.TotalStats.Mean.ToString("F4"),
                    r.TotalStats.Median.ToString("F4"),
                    r.TotalStats.P95.ToString("F4"),
                    r.TotalStats.P99.ToString("F4"),
                    r.TotalStats.Max.ToString("F4")
                }).ToList()
            );

            Console.WriteLine();
            Console.WriteLine("  CHANGE DETECTION SUMMARY");
            PrintTable(
                new[] { "Scenario", "Total Changes", "Mean Depth", "Mean/Tick" },
                new[] { 11, 14, 11, 11 },
                results.Select(r => new[]
                {
                    r.ScenarioName,
                    r.TotalChangesDetected.ToString(),
                    r.PropagationDepthStats.Mean.ToString("F2"),
                    r.MeanChangesPerTick.ToString("F1")
                }).ToList()
            );
        }

        public static void PrintDetailedResult(BenchmarkResult result)
        {
            Console.WriteLine();
            Console.WriteLine($"  -- {result.ScenarioName} " + new string('-', Math.Max(0, 74 - result.ScenarioName.Length)));
            Console.WriteLine($"  Tree: {result.NodeCount} nodes | Operations: {result.OperationCount} | Iterations: {result.Iterations}");
            Console.WriteLine();

            Console.WriteLine("  Tick Duration (ms):");
            PrintStatsLine(result.TickStats);

            Console.WriteLine("  Total Scenario Time (ms):");
            PrintStatsLine(result.TotalStats);

            Console.WriteLine("  Propagation Depth:");
            PrintStatsLine(result.PropagationDepthStats);

            Console.WriteLine();
            Console.WriteLine("  Changes by Operation Type:");
            var byOp = result.ChangesByOperationType();
            var depthByOp = result.MeanDepthByOperationType();
            if (byOp.Count > 0)
            {
                PrintTable(
                    new[] { "Operation Type", "Count", "Avg Depth" },
                    new[] { 18, 8, 11 },
                    byOp.Select(kvp => new[]
                    {
                        kvp.Key,
                        kvp.Value.ToString(),
                        depthByOp.ContainsKey(kvp.Key) ? depthByOp[kvp.Key].ToString("F2") : "N/A"
                    }).ToList()
                );
            }
            else
            {
                Console.WriteLine("    (no changes detected)");
            }
        }

        private static void PrintStatsLine(Stats stats)
        {
            Console.WriteLine($"    Min: {stats.Min:F4}  Mean: {stats.Mean:F4}  Median: {stats.Median:F4}  P95: {stats.P95:F4}  P99: {stats.P99:F4}  Max: {stats.Max:F4}");
            Console.WriteLine();
        }

        private static void PrintTable(string[] headers, int[] widths, List<string[]> rows)
        {
            var divider = "  +";
            var headerLine = "  |";
            for (int c = 0; c < headers.Length; c++)
            {
                divider += new string('-', widths[c] + 2) + "+";
                headerLine += " " + PadOrTruncate(headers[c], widths[c]) + " |";
            }

            Console.WriteLine(divider);
            Console.WriteLine(headerLine);
            Console.WriteLine(divider);

            foreach (var row in rows)
            {
                var line = "  |";
                for (int c = 0; c < headers.Length; c++)
                {
                    var val = c < row.Length ? row[c] : "";
                    line += " " + PadOrTruncate(val, widths[c]) + " |";
                }
                Console.WriteLine(line);
            }

            Console.WriteLine(divider);
        }

        private static string PadOrTruncate(string s, int width)
        {
            if (s == null) s = "";
            if (s.Length > width)
                return s.Substring(0, width);
            return s.PadRight(width);
        }

        public static void PrintFooter(double totalElapsedMs)
        {
            Console.WriteLine();
            Console.WriteLine($"  Total benchmark time: {totalElapsedMs:F1} ms ({totalElapsedMs / 1000.0:F2} s)");
            Console.WriteLine(Separator);
            Console.WriteLine();
        }
    }
}
