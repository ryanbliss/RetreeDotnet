// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using NUnit.Framework;
using RetreeCore.Tests.TestHelpers;

namespace RetreeCore.Tests
{
    [TestFixture]
    public class RetreeTransactionTests
    {
        [SetUp]
        public void SetUp() => Retree.Reset();

        [TearDown]
        public void TearDown() => Retree.Reset();

        #region RunTransaction

        [Test]
        public void RunTransaction_BatchesNodeChanges_IntoSingleEmission()
        {
            var node = new SimpleNode { count = 0, flag = false };
            var receivedArgs = new List<NodeChangedArgs>();
            node.OnNodeChanged(args => receivedArgs.Add(args));

            Retree.RunTransaction(() =>
            {
                node.count = 10;
                node.flag = true;
                Retree.Tick();
            });

            Assert.AreEqual(1, receivedArgs.Count, "Should coalesce into single emission");
            Assert.AreEqual(2, receivedArgs[0].Changes.Count, "Should contain both field changes");
        }

        [Test]
        public void RunTransaction_BatchesListMutations()
        {
            var list = new RetreeList<SimpleNode>();
            var receivedArgs = new List<NodeChangedArgs>();
            list.OnNodeChanged(args => receivedArgs.Add(args));

            Retree.RunTransaction(() =>
            {
                list.Add(new SimpleNode());
                list.Add(new SimpleNode());
            });

            // Mutations are queued and flushed at end of transaction
            Assert.AreEqual(1, receivedArgs.Count, "Should coalesce list mutations");
        }

        [Test]
        public void RunTransaction_NestedTransactions_EmitAtOutermost()
        {
            var node = new SimpleNode { count = 0 };
            var receivedArgs = new List<NodeChangedArgs>();
            node.OnNodeChanged(args => receivedArgs.Add(args));

            Retree.RunTransaction(() =>
            {
                node.count = 1;
                Retree.Tick();

                Retree.RunTransaction(() =>
                {
                    node.count = 2;
                    Retree.Tick();
                });

                // Inner transaction should not have flushed yet
                Assert.AreEqual(0, receivedArgs.Count, "Should not emit during nested transaction");
            });

            Assert.AreEqual(1, receivedArgs.Count, "Should emit after outermost transaction completes");
        }

        [Test]
        public void RunTransaction_TreeChanges_AreBatched()
        {
            var parent = new NodeWithChild();
            var child = new SimpleNode { count = 0 };
            parent.child = child;

            var treeArgs = new List<TreeChangedArgs>();
            parent.OnTreeChanged(args => treeArgs.Add(args));

            Retree.RunTransaction(() =>
            {
                child.count = 5;
                Retree.Tick();
            });

            Assert.AreEqual(1, treeArgs.Count, "Tree changes should be batched");
        }

        #endregion

        #region RunSilent

        [Test]
        public void RunSilent_SuppressesAllEvents()
        {
            var node = new SimpleNode { count = 0 };
            int nodeCallCount = 0;
            int treeCallCount = 0;
            node.OnNodeChanged(_ => nodeCallCount++);
            node.OnTreeChanged(_ => treeCallCount++);

            Retree.RunSilent(() =>
            {
                node.count = 99;
            });

            Assert.AreEqual(0, nodeCallCount, "NodeChanged should not fire during silent");
            Assert.AreEqual(0, treeCallCount, "TreeChanged should not fire during silent");
        }

        [Test]
        public void RunSilent_UpdatesSnapshots_NoRedetectionOnNextTick()
        {
            var node = new SimpleNode { count = 0 };
            int callCount = 0;
            node.OnNodeChanged(_ => callCount++);

            Retree.RunSilent(() =>
            {
                node.count = 50;
            });

            Assert.AreEqual(0, callCount, "No events during silent");

            // Next tick should NOT detect the change because RunSilent internally ticked
            // to absorb the snapshot before releasing silent mode.
            Retree.Tick();
            Assert.AreEqual(0, callCount, "Should not re-detect silently applied changes");
        }

        [Test]
        public void RunSilent_ListMutations_AreApplied_ButNoEvents()
        {
            var list = new RetreeList<SimpleNode>();
            int callCount = 0;
            list.OnNodeChanged(_ => callCount++);

            Retree.RunSilent(() =>
            {
                list.Add(new SimpleNode());
                list.Add(new SimpleNode());
            });

            Assert.AreEqual(0, callCount, "No events during silent");
            Assert.AreEqual(2, list.Count, "Mutations should still be applied");
        }

        [Test]
        public void RunSilent_Dict_AddValue_SuppressesTreeChanged()
        {
            // RunSilent suppresses synchronous EmitNodeChanged calls (e.g. dict.Add),
            // which prevents tree change propagation to any listener on the dict.
            var dict = new RetreeDictionary<string, NodeWithChild>();
            int callCount = 0;
            dict.OnTreeChanged(_ => callCount++);

            Retree.RunSilent(() =>
            {
                dict.Add("k", new NodeWithChild());
            });

            Assert.AreEqual(0, callCount, "OnTreeChanged on dict should not fire when item added during RunSilent");
        }

        [Test]
        public void RunSilent_ParentNode_WithDictChild_AddValue_SuppressesTreeChanged()
        {
            // Same as above but the listener is on the parent RetreeNode that owns the dict.
            var parent = new NodeWithDeepDict();
            int dictCallCount = 0;
            int parentCallCount = 0;
            parent.Entries.OnTreeChanged(_ => dictCallCount++);
            parent.OnTreeChanged(_ => parentCallCount++);

            Retree.RunSilent(() =>
            {
                parent.Entries["k"] = new NodeWithChild();
            });

            Assert.AreEqual(0, dictCallCount, "dict OnTreeChanged should not fire during RunSilent");
            Assert.AreEqual(0, parentCallCount, "parent OnTreeChanged should not fire during RunSilent");
        }

        [Test]
        public void RunSilent_Dict_DeepChildFieldChange_SuppressedAndNotRedetected()
        {
            // RunSilent internally ticks before and after the action, so field changes
            // made inside — including deep nested ones — are absorbed into snapshots silently.
            // The next external Tick() should not re-detect them.
            var dict = new RetreeDictionary<string, NodeWithChild>();
            var item = new NodeWithChild();
            var child = new SimpleNode { count = 0 };
            item.child = child;
            dict.Add("k", item);

            int callCount = 0;
            dict.OnTreeChanged(_ => callCount++);

            Retree.RunSilent(() =>
            {
                child.count = 99;
            });

            Assert.AreEqual(0, callCount, "OnTreeChanged should not fire during RunSilent");

            Retree.Tick(); // post-tick inside RunSilent already updated the snapshot
            Assert.AreEqual(0, callCount, "Should not re-detect silently applied deep changes");
        }

        #endregion
    }
}
