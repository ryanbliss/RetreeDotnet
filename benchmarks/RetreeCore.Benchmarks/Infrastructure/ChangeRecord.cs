// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace RetreeCore.Benchmarks.Infrastructure
{
    public readonly struct ChangeRecord
    {
        public long TimestampTicks { get; }
        public int TickNumber { get; }
        public string SourceNodeType { get; }
        public string FieldName { get; }
        public int PropagationDepth { get; }
        public string OperationType { get; }

        public ChangeRecord(long timestampTicks, int tickNumber, string sourceNodeType,
            string fieldName, int propagationDepth, string operationType)
        {
            TimestampTicks = timestampTicks;
            TickNumber = tickNumber;
            SourceNodeType = sourceNodeType;
            FieldName = fieldName;
            PropagationDepth = propagationDepth;
            OperationType = operationType;
        }

        public double TimestampMs => (double)TimestampTicks / Stopwatch.Frequency * 1000.0;
    }
}
