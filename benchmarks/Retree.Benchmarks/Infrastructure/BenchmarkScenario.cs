// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Retree.Benchmarks.Infrastructure
{
    public class BenchmarkScenario
    {
        public string Name { get; }
        public string TreeSize { get; }
        public string OpCategory { get; }
        public Func<RetreeBase> TreeFactory { get; }
        public Func<RetreeBase, Action[]> OpFactory { get; }
        public int WarmupIterations { get; }
        public int MeasuredIterations { get; }

        public BenchmarkScenario(
            string name,
            string treeSize,
            string opCategory,
            Func<RetreeBase> treeFactory,
            Func<RetreeBase, Action[]> opFactory,
            int warmupIterations = 5,
            int measuredIterations = 100)
        {
            Name = name;
            TreeSize = treeSize;
            OpCategory = opCategory;
            TreeFactory = treeFactory;
            OpFactory = opFactory;
            WarmupIterations = warmupIterations;
            MeasuredIterations = measuredIterations;
        }
    }
}
