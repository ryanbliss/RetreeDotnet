// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Retree
{
    public class NodeChangedArgs
    {
        public RetreeBase Node { get; }
        public IReadOnlyList<FieldChange> Changes { get; }

        public NodeChangedArgs(RetreeBase node, IReadOnlyList<FieldChange> changes)
        {
            Node = node;
            Changes = changes;
        }
    }
}
