// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace RetreeCore.Internal
{
    internal sealed class TransactionQueue
    {
        // Preserves insertion order while coalescing changes per node
        private readonly List<RetreeBase> _orderedNodes = new List<RetreeBase>();
        private readonly Dictionary<RetreeBase, List<FieldChange>> _nodeChanges = new Dictionary<RetreeBase, List<FieldChange>>();
        private readonly Dictionary<RetreeBase, List<TreeChangeEntry>> _treeChanges = new Dictionary<RetreeBase, List<TreeChangeEntry>>();

        internal readonly struct TreeChangeEntry
        {
            public RetreeBase ListenerNode { get; }
            public RetreeBase SourceNode { get; }
            public IReadOnlyList<FieldChange> Changes { get; }

            public TreeChangeEntry(RetreeBase listenerNode, RetreeBase sourceNode, IReadOnlyList<FieldChange> changes)
            {
                ListenerNode = listenerNode;
                SourceNode = sourceNode;
                Changes = changes;
            }
        }

        public void QueueNodeChange(RetreeBase node, IReadOnlyList<FieldChange> changes)
        {
            if (!_nodeChanges.TryGetValue(node, out var existing))
            {
                existing = new List<FieldChange>();
                _nodeChanges[node] = existing;
                if (!_treeChanges.ContainsKey(node))
                    _orderedNodes.Add(node);
            }
            existing.AddRange(changes);
        }

        public void QueueTreeChange(RetreeBase listenerNode, RetreeBase sourceNode, IReadOnlyList<FieldChange> changes)
        {
            if (!_treeChanges.TryGetValue(listenerNode, out var existing))
            {
                existing = new List<TreeChangeEntry>();
                _treeChanges[listenerNode] = existing;
                if (!_nodeChanges.ContainsKey(listenerNode))
                    _orderedNodes.Add(listenerNode);
            }
            existing.Add(new TreeChangeEntry(listenerNode, sourceNode, changes));
        }

        public void Flush()
        {
            // Copy to avoid issues if callbacks cause new queuing
            var nodes = new List<RetreeBase>(_orderedNodes);
            var nodeChanges = new Dictionary<RetreeBase, List<FieldChange>>(_nodeChanges);
            var treeChanges = new Dictionary<RetreeBase, List<TreeChangeEntry>>(_treeChanges);
            Clear();

            foreach (var node in nodes)
            {
                if (nodeChanges.TryGetValue(node, out var changes))
                {
                    node.EmitNodeChanged(new NodeChangedArgs(node, changes));
                }

                if (treeChanges.TryGetValue(node, out var treeEntries))
                {
                    // Emit once per listener node with the last source's changes
                    var lastEntry = treeEntries[treeEntries.Count - 1];
                    node.EmitTreeChanged(new TreeChangedArgs(lastEntry.ListenerNode, lastEntry.SourceNode, lastEntry.Changes));
                }
            }
        }

        public void Clear()
        {
            _orderedNodes.Clear();
            _nodeChanges.Clear();
            _treeChanges.Clear();
        }
    }
}
