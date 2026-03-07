// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RetreeCore.Benchmarks.Infrastructure;
using RetreeCore.Benchmarks.Models;

namespace RetreeCore.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = ParseArgs(args);

            var scenarios = BuildScenarios(options);
            if (scenarios.Count == 0)
            {
                Console.WriteLine("No scenarios matched the given filters.");
                return;
            }

            ConsoleReporter.PrintHeader(options.Iterations, options.Warmup);

            Console.WriteLine();
            Console.WriteLine($"  Running {scenarios.Count} scenario(s)...");
            Console.WriteLine();

            var overallTimer = new BenchmarkTimer();
            overallTimer.Start();

            var results = BenchmarkRunner.RunAll(scenarios, verbose: options.Verbose);

            overallTimer.Stop();

            // Fill in node counts
            foreach (var result in results)
            {
                // Re-build tree just to count nodes (lightweight)
                var matchingScenario = scenarios.Find(s => s.Name == result.ScenarioName);
                if (matchingScenario != null)
                {
                    Retree.Reset();
                    var tree = matchingScenario.TreeFactory();
                    // Register tree changed to activate nodes, then count
                    tree.RegisterOnTreeChanged(_ => { });
                    Retree.Tick();
                    result.NodeCount = BenchmarkRunner.CountNodes(tree);
                    result.OperationCount = matchingScenario.OpFactory(tree).Length;
                    Retree.ClearListeners(tree, recursive: true);
                    Retree.Reset();
                }
            }

            ConsoleReporter.PrintSummaryTable(results);

            if (options.Verbose)
            {
                foreach (var result in results)
                {
                    ConsoleReporter.PrintDetailedResult(result);
                }
            }

            ConsoleReporter.PrintFooter(overallTimer.ElapsedMs);
        }

        static List<BenchmarkScenario> BuildScenarios(Options options)
        {
            var scenarios = new List<BenchmarkScenario>();

            var sizeFilter = options.Size?.ToLowerInvariant() ?? "all";
            var opsFilter = options.Ops?.ToLowerInvariant() ?? "all";

            if (sizeFilter == "all" || sizeFilter == "small")
            {
                if (opsFilter == "all" || opsFilter == "low")
                    scenarios.Add(MakeScenario("Small/Low", "Small", "Low",
                        () => SmallTreeFactory.Build(), SmallTreeFactory.LowOps, options));
                if (opsFilter == "all" || opsFilter == "medium")
                    scenarios.Add(MakeScenario("Small/Med", "Small", "Med",
                        () => SmallTreeFactory.Build(), SmallTreeFactory.MediumOps, options));
                if (opsFilter == "all" || opsFilter == "high")
                    scenarios.Add(MakeScenario("Small/High", "Small", "High",
                        () => SmallTreeFactory.Build(), SmallTreeFactory.HighOps, options));
            }

            if (sizeFilter == "all" || sizeFilter == "medium")
            {
                if (opsFilter == "all" || opsFilter == "low")
                    scenarios.Add(MakeScenario("Med/Low", "Medium", "Low",
                        () => MediumTreeFactory.Build(), MediumTreeFactory.LowOps, options));
                if (opsFilter == "all" || opsFilter == "medium")
                    scenarios.Add(MakeScenario("Med/Med", "Medium", "Med",
                        () => MediumTreeFactory.Build(), MediumTreeFactory.MediumOps, options));
                if (opsFilter == "all" || opsFilter == "high")
                    scenarios.Add(MakeScenario("Med/High", "Medium", "High",
                        () => MediumTreeFactory.Build(), MediumTreeFactory.HighOps, options));
            }

            if (sizeFilter == "all" || sizeFilter == "large")
            {
                if (opsFilter == "all" || opsFilter == "low")
                    scenarios.Add(MakeScenario("Large/Low", "Large", "Low",
                        () => LargeTreeFactory.Build(), LargeTreeFactory.LowOps, options));
                if (opsFilter == "all" || opsFilter == "medium")
                    scenarios.Add(MakeScenario("Large/Med", "Large", "Med",
                        () => LargeTreeFactory.Build(), LargeTreeFactory.MediumOps, options));
                if (opsFilter == "all" || opsFilter == "high")
                    scenarios.Add(MakeScenario("Large/High", "Large", "High",
                        () => LargeTreeFactory.Build(), LargeTreeFactory.HighOps, options));
            }

            if (sizeFilter == "all" || sizeFilter == "xlarge")
            {
                if (opsFilter == "all" || opsFilter == "xhigh")
                    scenarios.Add(MakeScenario("XL/XHigh", "XLarge", "XHigh",
                        () => XLargeTreeFactory.Build(), XLargeTreeFactory.XHighOps, options));
            }

            return scenarios;
        }

        static BenchmarkScenario MakeScenario(string name, string treeSize, string opCategory,
            Func<RetreeBase> factory, Func<RetreeBase, Action[]> opFactory, Options options)
        {
            return new BenchmarkScenario(
                name: name,
                treeSize: treeSize,
                opCategory: opCategory,
                treeFactory: factory,
                opFactory: opFactory,
                warmupIterations: options.Warmup,
                measuredIterations: options.Iterations
            );
        }

        static Options ParseArgs(string[] args)
        {
            var options = new Options();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--size":
                        if (i + 1 < args.Length) options.Size = args[++i];
                        break;
                    case "--ops":
                        if (i + 1 < args.Length) options.Ops = args[++i];
                        break;
                    case "--iterations":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out var iters))
                            options.Iterations = iters;
                        break;
                    case "--warmup":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out var warmup))
                            options.Warmup = warmup;
                        break;
                    case "--verbose":
                        options.Verbose = true;
                        break;
                    case "--help":
                    case "-h":
                        PrintHelp();
                        Environment.Exit(0);
                        break;
                }
            }
            return options;
        }

        static void PrintHelp()
        {
            Console.WriteLine("Usage: dotnet run --project DotnetTests/Benchmarks -- [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --size <small|medium|large|xlarge|all>  Tree size filter (default: all)");
            Console.WriteLine("  --ops <low|medium|high|xhigh|all>      Operation count filter (default: all)");
            Console.WriteLine("  --iterations <N>                    Measured iterations (default: 100)");
            Console.WriteLine("  --warmup <N>                        Warmup iterations (default: 5)");
            Console.WriteLine("  --verbose                           Print detailed per-scenario breakdown");
            Console.WriteLine("  --help, -h                          Show this help");
        }

        class Options
        {
            public string Size = "all";
            public string Ops = "all";
            public int Iterations = 100;
            public int Warmup = 5;
            public bool Verbose = false;
        }
    }
}
