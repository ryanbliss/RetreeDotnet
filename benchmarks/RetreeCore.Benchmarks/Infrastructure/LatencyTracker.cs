// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RetreeCore.Benchmarks.Infrastructure
{
    public class LatencyTracker
    {
        private readonly List<ChangeRecord> _records = new List<ChangeRecord>();
        private Action<TreeChangedArgs> _listener;
        private RetreeBase _root;
        private int _currentTick;

        // Maps source node type name → operation type label (set externally before mutations)
        private readonly Dictionary<string, string> _opTypeOverrides = new Dictionary<string, string>();

        public IReadOnlyList<ChangeRecord> Records => _records;
        public int CurrentTick => _currentTick;

        public void Attach(RetreeBase root)
        {
            _root = root;
            _listener = OnTreeChanged;
            root.OnTreeChanged(_listener);
        }

        public void Detach()
        {
            if (_root != null && _listener != null)
            {
                _root.OffTreeChanged(_listener);
                _root = null;
                _listener = null;
            }
        }

        public void OnTickStart()
        {
            _currentTick++;
        }

        public void SetOperationType(string nodeTypeName, string operationType)
        {
            _opTypeOverrides[nodeTypeName] = operationType;
        }

        public void ClearOperationTypes()
        {
            _opTypeOverrides.Clear();
        }

        public void Clear()
        {
            _records.Clear();
            _currentTick = 0;
            _opTypeOverrides.Clear();
        }

        public List<ChangeRecord> DrainRecords()
        {
            var copy = new List<ChangeRecord>(_records);
            _records.Clear();
            return copy;
        }

        private void OnTreeChanged(TreeChangedArgs args)
        {
            var timestamp = Stopwatch.GetTimestamp();
            var sourceType = args.SourceNode?.GetType().Name ?? "Unknown";
            int depth = ComputeDepth(args.SourceNode, args.ListenerNode);

            string opType = "Unknown";
            if (_opTypeOverrides.TryGetValue(sourceType, out var overrideType))
                opType = overrideType;
            else
                opType = ClassifyChange(args);

            foreach (var change in args.Changes)
            {
                _records.Add(new ChangeRecord(
                    timestampTicks: timestamp,
                    tickNumber: _currentTick,
                    sourceNodeType: sourceType,
                    fieldName: change.FieldName,
                    propagationDepth: depth,
                    operationType: opType
                ));
            }
        }

        private string ClassifyChange(TreeChangedArgs args)
        {
            if (args.Changes.Count == 0) return "Empty";

            var firstChange = args.Changes[0];
            if (firstChange.FieldName == "Items")
                return firstChange.OldValue == null ? "ListAdd" : "ListRemove";

            // Dictionary mutations use key.ToString() as field name
            if (args.SourceNode is RetreeBase && !(args.SourceNode is RetreeNode))
                return "DictMutation";

            return "FieldChange";
        }

        private static int ComputeDepth(RetreeBase source, RetreeBase listener)
        {
            if (source == null || listener == null) return 0;
            if (ReferenceEquals(source, listener)) return 0;

            int depth = 0;
            var current = source;
            while (current != null && !ReferenceEquals(current, listener))
            {
                current = Retree.Parent(current);
                depth++;
                if (depth > 100) break; // safety
            }
            return depth;
        }
    }
}
