// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using RetreeCore.Tests.TestHelpers;

namespace RetreeCore.Tests
{
    [TestFixture]
    public class RetreeParentTests
    {
        [SetUp]
        public void SetUp() => Retree.Reset();

        [TearDown]
        public void TearDown() => Retree.Reset();

        [Test]
        public void RetreeList_Add_SetsParent_ToList()
        {
            var list = new RetreeList<SimpleNode>();
            var item = new SimpleNode();
            list.Add(item);

            Assert.AreSame(list, Retree.Parent(item));
        }

        [Test]
        public void RetreeList_Remove_ClearsParent()
        {
            var list = new RetreeList<SimpleNode>();
            var item = new SimpleNode();
            list.Add(item);
            list.Remove(item);

            Assert.IsNull(Retree.Parent(item));
        }

        [Test]
        public void RetreeDictionary_Add_SetsParent()
        {
            var dict = new RetreeDictionary<string, SimpleNode>();
            var item = new SimpleNode();
            dict.Add("k", item);

            Assert.AreSame(dict, Retree.Parent(item));
        }

        [Test]
        public void RetreeNode_ChildField_SetsParent_WhenListenerActive()
        {
            var parent = new NodeWithChild();
            var child = new SimpleNode();
            parent.child = child;

            // Parent tracking for fields happens during snapshot (first listener registration)
            parent.OnNodeChanged(_ => { });

            Assert.AreSame(parent, Retree.Parent(child));
        }

        [Test]
        public void RetreeNode_ChildField_ChangedToNew_UpdatesParent()
        {
            var parent = new NodeWithChild();
            var oldChild = new SimpleNode();
            parent.child = oldChild;

            parent.OnNodeChanged(_ => { });
            Assert.AreSame(parent, Retree.Parent(oldChild));

            var newChild = new SimpleNode();
            parent.child = newChild;
            Retree.Tick();

            Assert.IsNull(Retree.Parent(oldChild));
            Assert.AreSame(parent, Retree.Parent(newChild));
        }

        [Test]
        public void RetreeNode_ChildField_SetToNull_ClearsParent()
        {
            var parent = new NodeWithChild();
            var child = new SimpleNode();
            parent.child = child;

            parent.OnNodeChanged(_ => { });
            Assert.AreSame(parent, Retree.Parent(child));

            parent.child = null;
            Retree.Tick();

            Assert.IsNull(Retree.Parent(child));
        }

        [Test]
        public void SingleParentRule_AddToTwoLists_Throws()
        {
            var list1 = new RetreeList<SimpleNode>();
            var list2 = new RetreeList<SimpleNode>();
            var item = new SimpleNode();

            list1.Add(item);
            Assert.Throws<System.InvalidOperationException>(() => list2.Add(item));
        }

        [Test]
        public void Reparenting_RemoveThenAdd_Works()
        {
            var list1 = new RetreeList<SimpleNode>();
            var list2 = new RetreeList<SimpleNode>();
            var item = new SimpleNode();

            list1.Add(item);
            Assert.AreSame(list1, Retree.Parent(item));

            list1.Remove(item);
            Assert.IsNull(Retree.Parent(item));

            list2.Add(item);
            Assert.AreSame(list2, Retree.Parent(item));
        }

        [Test]
        public void NewNode_HasNoParent()
        {
            var node = new SimpleNode();
            Assert.IsNull(Retree.Parent(node));
        }

        [Test]
        public void RetreeList_Clear_ClearsAllParents()
        {
            var list = new RetreeList<SimpleNode>();
            var a = new SimpleNode();
            var b = new SimpleNode();
            list.Add(a);
            list.Add(b);

            list.Clear();

            Assert.IsNull(Retree.Parent(a));
            Assert.IsNull(Retree.Parent(b));
        }
    }
}
