// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Retree
{
    public class TreeChangedArgs
    {
        public RetreeBase ListenerNode { get; }
        public RetreeBase SourceNode { get; }
        public IReadOnlyList<FieldChange> Changes { get; }

        public TreeChangedArgs(RetreeBase listenerNode, RetreeBase sourceNode, IReadOnlyList<FieldChange> changes)
        {
            ListenerNode = listenerNode;
            SourceNode = sourceNode;
            Changes = changes;
        }
    }
}
