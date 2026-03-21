// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

namespace RetreeCore.Tests.TestHelpers
{
    public class SimpleNode : RetreeNode
    {
        public int count = 0;
        public string label = "";
        public bool flag = false;
    }

    public class NodeWithChild : RetreeNode
    {
        public SimpleNode child;
        public int value = 0;
    }

    public class NodeWithList : RetreeNode
    {
        private RetreeList<SimpleNode> _items = new RetreeList<SimpleNode>();
        public RetreeList<SimpleNode> Items => _items;
    }

    public class NodeWithDict : RetreeNode
    {
        private RetreeDictionary<string, SimpleNode> _entries = new RetreeDictionary<string, SimpleNode>();
        public RetreeDictionary<string, SimpleNode> Entries => _entries;
    }

    public class NodeWithDeepDict : RetreeNode
    {
        private RetreeDictionary<string, NodeWithChild> _entries = new RetreeDictionary<string, NodeWithChild>();
        public RetreeDictionary<string, NodeWithChild> Entries => _entries;
    }

    public class NodeWithIgnored : RetreeNode
    {
        public int tracked = 0;

        [RetreeIgnore]
        public int ignored = 0;
    }

    public class NodeWithReadonly : RetreeNode
    {
        public int mutable = 0;
        public readonly int immutable = 42;
    }

    public class NodeWithProperty : RetreeNode
    {
        public int field = 0;
        public int Property { get; set; } // should not be observed
    }

    public class DeepChild : RetreeNode
    {
        public int depth = 0;
    }

    public class MiddleNode : RetreeNode
    {
        public DeepChild child;
    }

    public class RootNode : RetreeNode
    {
        public MiddleNode middle;
        private RetreeList<MiddleNode> _middles = new RetreeList<MiddleNode>();
        public RetreeList<MiddleNode> Middles => _middles;
    }
}
