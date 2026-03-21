// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using RetreeCore.Internal;

namespace RetreeCore
{
    public static class Retree
    {
        private static Timer _tickTimer;
        private static bool _isTicking;
        private static int _transactionDepth;
        private static bool _isSilent;
        private static TransactionQueue _transactionQueue;

        public static bool IsTicking => _isTicking;
        internal static bool InTransaction => _transactionDepth > 0;
        internal static bool IsSilent => _isSilent;

        public static RetreeBase Parent(RetreeBase node)
        {
            return node._parent;
        }

        public static void Tick()
        {
            // Copy to avoid modification during iteration
            var nodes = new List<RetreeNode>(RetreeRegistry.ActiveNodes);
            foreach (var node in nodes)
            {
                node.PollForChanges();
            }
        }

        public static void StartTicks(float tickRateSeconds)
        {
            StopTicks();

            var intervalMs = (int)(tickRateSeconds * 1000);
            if (intervalMs <= 0) intervalMs = 1;

            _tickTimer = new Timer(_ => Tick(), null, intervalMs, intervalMs);
            _isTicking = true;
        }

        public static void StopTicks()
        {
            if (_tickTimer != null)
            {
                _tickTimer.Dispose();
                _tickTimer = null;
            }
            _isTicking = false;
        }

        public static void RunTransaction(Action action)
        {
            _transactionDepth++;
            if (_transactionDepth == 1)
                _transactionQueue = new TransactionQueue();

            try
            {
                action();
            }
            finally
            {
                _transactionDepth--;
                if (_transactionDepth == 0)
                {
                    var queue = _transactionQueue;
                    _transactionQueue = null;
                    queue.Flush();
                }
            }
        }

        public static void RunSilent(Action action)
        {
            var wasSilent = _isSilent;
            // Pre-tick: flush any pending changes normally so they fire before silent mode begins.
            Tick();
            _isSilent = true;
            try
            {
                action();
                // Post-tick: absorb mutations made inside the action into snapshots silently
                // so they are not re-detected on the next external tick.
                Tick();
            }
            finally
            {
                _isSilent = wasSilent;
            }
        }

        public static void ClearAllListeners()
        {
            // Copy to avoid modification during iteration (ClearAllListeners unregisters nodes)
            var nodes = new List<RetreeNode>(RetreeRegistry.ActiveNodes);
            foreach (var node in nodes)
            {
                ClearListeners(node, true);
            }
        }

        public static void ClearListeners(RetreeBase node, bool recursive = false)
        {
            node.ClearAllListeners();

            if (recursive)
            {
                if (node is RetreeNode retreeNode)
                {
                    var fields = FieldCache.GetFields(retreeNode.GetType());
                    foreach (var field in fields)
                    {
                        if (field.Kind == FieldKind.Value) continue;
                        var child = field.Getter(retreeNode) as RetreeBase;
                        if (child != null)
                            ClearListeners(child, true);
                    }
                }
                else if (node is IEnumerable<RetreeNode> nodeList)
                {
                    foreach (var child in nodeList)
                    {
                        if (child != null)
                            ClearListeners(child, true);
                    }
                }
            }
        }

        internal static void QueueNodeChange(RetreeBase node, IReadOnlyList<FieldChange> changes)
        {
            _transactionQueue?.QueueNodeChange(node, changes);
        }

        internal static void QueueTreeChange(RetreeBase listenerNode, RetreeBase sourceNode, IReadOnlyList<FieldChange> changes)
        {
            _transactionQueue?.QueueTreeChange(listenerNode, sourceNode, changes);
        }

        public static void Reset()
        {
            StopTicks();
            _transactionDepth = 0;
            _isSilent = false;
            _transactionQueue = null;
            RetreeRegistry.Reset();
            FieldCache.Reset();
        }
    }
}
