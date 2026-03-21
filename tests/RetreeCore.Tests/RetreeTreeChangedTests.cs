// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using NUnit.Framework;
using RetreeCore.Tests.TestHelpers;

namespace RetreeCore.Tests
{
    [TestFixture]
    public class RetreeTreeChangedTests
    {
        [SetUp]
        public void SetUp() => Retree.Reset();

        [TearDown]
        public void TearDown() => Retree.Reset();

        [Test]
        public void TreeChanged_FiresWhenOwnFieldChanges()
        {
            var node = new SimpleNode { count = 0 };
            TreeChangedArgs received = null;
            node.OnTreeChanged(args => received = args);

            node.count = 5;
            Retree.Tick();

            Assert.IsNotNull(received);
            Assert.AreSame(node, received.ListenerNode);
            Assert.AreSame(node, received.SourceNode);
            Assert.AreEqual("count", received.Changes[0].FieldName);
        }

        [Test]
        public void TreeChanged_OnParent_WhenChildFieldChanges()
        {
            var parent = new NodeWithChild();
            var child = new SimpleNode { count = 0 };
            parent.child = child;

            TreeChangedArgs received = null;
            parent.OnTreeChanged(args => received = args);

            child.count = 10;
            Retree.Tick();

            Assert.IsNotNull(received);
            Assert.AreSame(parent, received.ListenerNode);
            Assert.AreSame(child, received.SourceNode);
        }

        [Test]
        public void TreeChanged_OnNodeWithList_WhenItemAdded()
        {
            var parent = new NodeWithList();
            TreeChangedArgs received = null;
            parent.OnTreeChanged(args => received = args);

            parent.Items.Add(new SimpleNode());

            Assert.IsNotNull(received, "TreeChanged should fire when item added to child list");
        }

        [Test]
        public void TreeChanged_OnNodeWithList_WhenItemFieldChanges()
        {
            var parent = new NodeWithList();
            var item = new SimpleNode { count = 0 };
            parent.Items.Add(item);

            var receivedArgs = new List<TreeChangedArgs>();
            parent.OnTreeChanged(args => receivedArgs.Add(args));

            item.count = 42;
            Retree.Tick();

            Assert.IsTrue(receivedArgs.Count > 0, "TreeChanged should fire on parent when list item field changes");

            // Find the change that originated from the item
            bool foundItemChange = false;
            foreach (var args in receivedArgs)
            {
                if (ReferenceEquals(args.SourceNode, item))
                {
                    foundItemChange = true;
                    break;
                }
            }
            Assert.IsTrue(foundItemChange, "Should include change sourced from the item");
        }

        [Test]
        public void TreeChanged_DeepNesting_PropagatesUp()
        {
            var root = new RootNode();
            var middle = new MiddleNode();
            var deep = new DeepChild { depth = 0 };
            root.middle = middle;
            middle.child = deep;

            TreeChangedArgs received = null;
            root.OnTreeChanged(args => received = args);

            deep.depth = 99;
            Retree.Tick();

            Assert.IsNotNull(received);
            Assert.AreSame(root, received.ListenerNode);
            Assert.AreSame(deep, received.SourceNode);
        }

        [Test]
        public void TreeChanged_NewChildAddedToList_IsTracked()
        {
            var parent = new NodeWithList();
            parent.OnTreeChanged(_ => { });

            var item = new SimpleNode { count = 0 };
            parent.Items.Add(item);

            // Now change the item's field
            TreeChangedArgs received = null;
            parent.OnTreeChanged(args => received = args);

            item.count = 5;
            Retree.Tick();

            Assert.IsNotNull(received, "Newly added list item changes should propagate as tree change");
        }

        [Test]
        public void TreeChanged_RemovedChildFromList_IsNoLongerTracked()
        {
            var parent = new NodeWithList();
            var item = new SimpleNode { count = 0 };
            parent.Items.Add(item);

            parent.OnTreeChanged(_ => { });

            parent.Items.Remove(item);

            // Now change the removed item
            var receivedAfterRemove = new List<TreeChangedArgs>();
            parent.OnTreeChanged(args => receivedAfterRemove.Add(args));

            item.count = 99;
            Retree.Tick();

            // Should not get a tree change sourced from the removed item
            bool foundRemovedItemChange = false;
            foreach (var args in receivedAfterRemove)
            {
                if (ReferenceEquals(args.SourceNode, item))
                {
                    foundRemovedItemChange = true;
                    break;
                }
            }
            Assert.IsFalse(foundRemovedItemChange, "Removed item changes should not propagate to parent");
        }

        [Test]
        public void TreeChanged_ChildNodeFieldSwapped_TracksNewChild()
        {
            var parent = new NodeWithChild();
            var oldChild = new SimpleNode { count = 0 };
            var newChild = new SimpleNode { count = 0 };
            parent.child = oldChild;

            parent.OnTreeChanged(_ => { });

            // Swap child
            parent.child = newChild;
            Retree.Tick();

            // Change the new child
            TreeChangedArgs received = null;
            parent.OnTreeChanged(args => received = args);

            newChild.count = 42;
            Retree.Tick();

            Assert.IsNotNull(received, "New child should be tracked after swap");
            Assert.AreSame(newChild, received.SourceNode);
        }

        [Test]
        public void TreeChanged_OnList_DirectlyRegistered()
        {
            var list = new RetreeList<SimpleNode>();
            var item = new SimpleNode { count = 0 };
            list.Add(item);

            TreeChangedArgs received = null;
            list.OnTreeChanged(args => received = args);

            item.count = 10;
            Retree.Tick();

            Assert.IsNotNull(received, "TreeChanged on list should fire when item changes");
            Assert.AreSame(list, received.ListenerNode);
        }

        [Test]
        public void TreeChanged_OnDict_DirectlyRegistered()
        {
            var dict = new RetreeDictionary<string, SimpleNode>();
            var item = new SimpleNode { count = 0 };
            dict.Add("k", item);

            TreeChangedArgs received = null;
            dict.OnTreeChanged(args => received = args);

            item.count = 10;
            Retree.Tick();

            Assert.IsNotNull(received, "TreeChanged on dict should fire when value changes");
            Assert.AreSame(dict, received.ListenerNode);
        }

        [Test]
        public void TreeChanged_OnDict_DeepChild_FieldChange_Propagates()
        {
            // Regression: dict → item (NodeWithChild) → item.child (SimpleNode) → child.count
            // Previously, changes to item.child.count were not detected because SubscribeToValue
            // did not recursively enable tree listening (SubscribeToChildren) on each value.
            var dict = new RetreeDictionary<string, NodeWithChild>();
            var item = new NodeWithChild();
            var child = new SimpleNode { count = 0 };
            item.child = child;
            dict.Add("k", item);

            TreeChangedArgs received = null;
            dict.OnTreeChanged(args => received = args);

            child.count = 99;
            Retree.Tick();

            Assert.IsNotNull(received, "TreeChanged on dict should fire when a value's child RetreeNode field changes");
            Assert.AreSame(dict, received.ListenerNode);
            Assert.AreSame(child, received.SourceNode);
        }

        [Test]
        public void TreeChanged_OnList_DeepChild_FieldChange_Propagates()
        {
            // Regression: list → item (NodeWithChild) → item.child (SimpleNode) → child.count
            var list = new RetreeList<NodeWithChild>();
            var item = new NodeWithChild();
            var child = new SimpleNode { count = 0 };
            item.child = child;
            list.Add(item);

            TreeChangedArgs received = null;
            list.OnTreeChanged(args => received = args);

            child.count = 42;
            Retree.Tick();

            Assert.IsNotNull(received, "TreeChanged on list should fire when an item's child RetreeNode field changes");
            Assert.AreSame(list, received.ListenerNode);
            Assert.AreSame(child, received.SourceNode);
        }

        [Test]
        public void UnregisterTreeChanged_StopsPropagation()
        {
            var parent = new NodeWithChild();
            var child = new SimpleNode { count = 0 };
            parent.child = child;

            int callCount = 0;
            void Listener(TreeChangedArgs args) => callCount++;
            parent.OnTreeChanged(Listener);

            child.count = 1;
            Retree.Tick();
            Assert.AreEqual(1, callCount);

            parent.OffTreeChanged(Listener);

            child.count = 2;
            Retree.Tick();
            Assert.AreEqual(1, callCount, "Should not fire after unregister");
        }
    }
}
