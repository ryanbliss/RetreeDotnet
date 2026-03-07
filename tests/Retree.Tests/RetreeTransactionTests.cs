// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using NUnit.Framework;
using Retree.Tests.TestHelpers;

namespace Retree.Tests
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
            node.RegisterOnNodeChanged(args => receivedArgs.Add(args));

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
            list.RegisterOnNodeChanged(args => receivedArgs.Add(args));

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
            node.RegisterOnNodeChanged(args => receivedArgs.Add(args));

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
            parent.RegisterOnTreeChanged(args => treeArgs.Add(args));

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
            node.RegisterOnNodeChanged(_ => nodeCallCount++);
            node.RegisterOnTreeChanged(_ => treeCallCount++);

            Retree.RunSilent(() =>
            {
                node.count = 99;
                Retree.Tick();
            });

            Assert.AreEqual(0, nodeCallCount, "NodeChanged should not fire during silent");
            Assert.AreEqual(0, treeCallCount, "TreeChanged should not fire during silent");
        }

        [Test]
        public void RunSilent_UpdatesSnapshots_NoRedetectionOnNextTick()
        {
            var node = new SimpleNode { count = 0 };
            int callCount = 0;
            node.RegisterOnNodeChanged(_ => callCount++);

            Retree.RunSilent(() =>
            {
                node.count = 50;
                Retree.Tick();
            });

            Assert.AreEqual(0, callCount, "No events during silent");

            // Next tick should NOT detect the change because snapshot was updated
            Retree.Tick();
            Assert.AreEqual(0, callCount, "Should not re-detect silently applied changes");
        }

        [Test]
        public void RunSilent_ListMutations_AreApplied_ButNoEvents()
        {
            var list = new RetreeList<SimpleNode>();
            int callCount = 0;
            list.RegisterOnNodeChanged(_ => callCount++);

            Retree.RunSilent(() =>
            {
                list.Add(new SimpleNode());
                list.Add(new SimpleNode());
            });

            Assert.AreEqual(0, callCount, "No events during silent");
            Assert.AreEqual(2, list.Count, "Mutations should still be applied");
        }

        #endregion
    }
}
