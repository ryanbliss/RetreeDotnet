// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace RetreeCore.Internal
{
    internal static class RetreeRegistry
    {
        private static readonly HashSet<RetreeNode> _activeNodes = new HashSet<RetreeNode>();

        public static void Register(RetreeNode node)
        {
            _activeNodes.Add(node);
        }

        public static void Unregister(RetreeNode node)
        {
            _activeNodes.Remove(node);
        }

        public static IReadOnlyCollection<RetreeNode> ActiveNodes => _activeNodes;

        internal static void Reset()
        {
            _activeNodes.Clear();
        }
    }
}
