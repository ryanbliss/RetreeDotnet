// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using NUnit.Framework;
using RetreeCore.Tests.TestHelpers;

namespace RetreeCore.Tests
{
    [TestFixture]
    public class RetreeListTests
    {
        [SetUp]
        public void SetUp() => Retree.Reset();

        [TearDown]
        public void TearDown() => Retree.Reset();

        [Test]
        public void Add_FiresOnNodeChanged_WithCorrectFieldChange()
        {
            var list = new RetreeList<SimpleNode>();
            NodeChangedArgs received = null;
            list.RegisterOnNodeChanged(args => received = args);

            var item = new SimpleNode { count = 1 };
            list.Add(item);

            Assert.IsNotNull(received);
            Assert.AreEqual(1, received.Changes.Count);
            Assert.AreEqual("Items", received.Changes[0].FieldName);
            Assert.IsNull(received.Changes[0].OldValue);
            Assert.AreSame(item, received.Changes[0].NewValue);
        }

        [Test]
        public void Remove_FiresOnNodeChanged()
        {
            var item = new SimpleNode();
            var list = new RetreeList<SimpleNode>();
            list.Add(item); // no listeners yet

            NodeChangedArgs received = null;
            list.RegisterOnNodeChanged(args => received = args);

            list.Remove(item);

            Assert.IsNotNull(received);
            Assert.AreEqual("Items", received.Changes[0].FieldName);
            Assert.AreSame(item, received.Changes[0].OldValue);
            Assert.IsNull(received.Changes[0].NewValue);
        }

        [Test]
        public void RemoveAt_FiresOnNodeChanged()
        {
            var item = new SimpleNode();
            var list = new RetreeList<SimpleNode>();
            list.Add(item);

            NodeChangedArgs received = null;
            list.RegisterOnNodeChanged(args => received = args);

            list.RemoveAt(0);

            Assert.IsNotNull(received);
            Assert.AreSame(item, received.Changes[0].OldValue);
        }

        [Test]
        public void Insert_FiresOnNodeChanged()
        {
            var list = new RetreeList<SimpleNode>();
            list.RegisterOnNodeChanged(_ => { });

            var item = new SimpleNode();
            NodeChangedArgs received = null;
            list.RegisterOnNodeChanged(args => received = args);

            list.Insert(0, item);

            Assert.IsNotNull(received);
            Assert.AreSame(item, received.Changes[0].NewValue);
        }

        [Test]
        public void IndexSetter_FiresOnNodeChanged_WithOldAndNew()
        {
            var old = new SimpleNode { count = 1 };
            var list = new RetreeList<SimpleNode>();
            list.Add(old);

            NodeChangedArgs received = null;
            list.RegisterOnNodeChanged(args => received = args);

            var replacement = new SimpleNode { count = 2 };
            list[0] = replacement;

            Assert.IsNotNull(received);
            Assert.AreSame(old, received.Changes[0].OldValue);
            Assert.AreSame(replacement, received.Changes[0].NewValue);
        }

        [Test]
        public void Clear_FiresOnNodeChanged_WithOneChangePerItem()
        {
            var a = new SimpleNode();
            var b = new SimpleNode();
            var list = new RetreeList<SimpleNode>();
            list.Add(a);
            list.Add(b);

            NodeChangedArgs received = null;
            list.RegisterOnNodeChanged(args => received = args);

            list.Clear();

            Assert.IsNotNull(received);
            Assert.AreEqual(2, received.Changes.Count);
        }

        [Test]
        public void Add_SetsParent()
        {
            var list = new RetreeList<SimpleNode>();
            var item = new SimpleNode();

            list.Add(item);

            Assert.AreSame(list, Retree.Parent(item));
        }

        [Test]
        public void Remove_ClearsParent()
        {
            var list = new RetreeList<SimpleNode>();
            var item = new SimpleNode();
            list.Add(item);

            list.Remove(item);

            Assert.IsNull(Retree.Parent(item));
        }

        [Test]
        public void Add_NodeWithExistingParent_Throws()
        {
            var list1 = new RetreeList<SimpleNode>();
            var list2 = new RetreeList<SimpleNode>();
            var item = new SimpleNode();

            list1.Add(item);

            Assert.Throws<System.InvalidOperationException>(() => list2.Add(item));
        }

        [Test]
        public void NoListeners_Add_DoesNotThrow()
        {
            var list = new RetreeList<SimpleNode>();
            Assert.DoesNotThrow(() => list.Add(new SimpleNode()));
            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public void MutationIsSynchronous_NoTickRequired()
        {
            var list = new RetreeList<SimpleNode>();
            int callCount = 0;
            list.RegisterOnNodeChanged(_ => callCount++);

            list.Add(new SimpleNode());

            Assert.AreEqual(1, callCount, "Event should fire synchronously, no Tick needed");
        }

        [Test]
        public void IListInterface_Works()
        {
            var list = new RetreeList<SimpleNode>();
            var item = new SimpleNode();
            list.Add(item);

            Assert.AreEqual(1, list.Count);
            Assert.IsTrue(list.Contains(item));
            Assert.AreEqual(0, list.IndexOf(item));
            Assert.AreSame(item, list[0]);
        }
    }
}
