// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using Retree.Tests.TestHelpers;

namespace Retree.Tests
{
    [TestFixture]
    public class RetreeDictionaryTests
    {
        [SetUp]
        public void SetUp() => Retree.Reset();

        [TearDown]
        public void TearDown() => Retree.Reset();

        [Test]
        public void Add_FiresOnNodeChanged_WithKeyAsFieldName()
        {
            var dict = new RetreeDictionary<string, SimpleNode>();
            NodeChangedArgs received = null;
            dict.RegisterOnNodeChanged(args => received = args);

            var item = new SimpleNode();
            dict.Add("myKey", item);

            Assert.IsNotNull(received);
            Assert.AreEqual(1, received.Changes.Count);
            Assert.AreEqual("myKey", received.Changes[0].FieldName);
            Assert.IsNull(received.Changes[0].OldValue);
            Assert.AreSame(item, received.Changes[0].NewValue);
        }

        [Test]
        public void Remove_FiresOnNodeChanged()
        {
            var dict = new RetreeDictionary<string, SimpleNode>();
            var item = new SimpleNode();
            dict.Add("k", item);

            NodeChangedArgs received = null;
            dict.RegisterOnNodeChanged(args => received = args);

            dict.Remove("k");

            Assert.IsNotNull(received);
            Assert.AreEqual("k", received.Changes[0].FieldName);
            Assert.AreSame(item, received.Changes[0].OldValue);
            Assert.IsNull(received.Changes[0].NewValue);
        }

        [Test]
        public void IndexerReplace_FiresWithOldAndNew()
        {
            var dict = new RetreeDictionary<string, SimpleNode>();
            var old = new SimpleNode { count = 1 };
            dict.Add("k", old);

            NodeChangedArgs received = null;
            dict.RegisterOnNodeChanged(args => received = args);

            var replacement = new SimpleNode { count = 2 };
            dict["k"] = replacement;

            Assert.IsNotNull(received);
            Assert.AreSame(old, received.Changes[0].OldValue);
            Assert.AreSame(replacement, received.Changes[0].NewValue);
        }

        [Test]
        public void IndexerNewKey_FiresAdd()
        {
            var dict = new RetreeDictionary<string, SimpleNode>();
            NodeChangedArgs received = null;
            dict.RegisterOnNodeChanged(args => received = args);

            var item = new SimpleNode();
            dict["newKey"] = item;

            Assert.IsNotNull(received);
            Assert.AreEqual("newKey", received.Changes[0].FieldName);
            Assert.AreSame(item, received.Changes[0].NewValue);
        }

        [Test]
        public void Clear_FiresOneChangePerEntry()
        {
            var dict = new RetreeDictionary<string, SimpleNode>();
            dict.Add("a", new SimpleNode());
            dict.Add("b", new SimpleNode());

            NodeChangedArgs received = null;
            dict.RegisterOnNodeChanged(args => received = args);

            dict.Clear();

            Assert.IsNotNull(received);
            Assert.AreEqual(2, received.Changes.Count);
        }

        [Test]
        public void Add_SetsParent()
        {
            var dict = new RetreeDictionary<string, SimpleNode>();
            var item = new SimpleNode();
            dict.Add("k", item);

            Assert.AreSame(dict, Retree.Parent(item));
        }

        [Test]
        public void Remove_ClearsParent()
        {
            var dict = new RetreeDictionary<string, SimpleNode>();
            var item = new SimpleNode();
            dict.Add("k", item);

            dict.Remove("k");

            Assert.IsNull(Retree.Parent(item));
        }

        [Test]
        public void Add_NodeWithExistingParent_Throws()
        {
            var dict1 = new RetreeDictionary<string, SimpleNode>();
            var dict2 = new RetreeDictionary<string, SimpleNode>();
            var item = new SimpleNode();

            dict1.Add("k", item);

            Assert.Throws<System.InvalidOperationException>(() => dict2.Add("k", item));
        }

        [Test]
        public void IndexerReplace_ClearsOldParent_SetsNewParent()
        {
            var dict = new RetreeDictionary<string, SimpleNode>();
            var old = new SimpleNode();
            dict.Add("k", old);

            var replacement = new SimpleNode();
            dict["k"] = replacement;

            Assert.IsNull(Retree.Parent(old));
            Assert.AreSame(dict, Retree.Parent(replacement));
        }

        [Test]
        public void IntKey_UsesToString()
        {
            var dict = new RetreeDictionary<int, SimpleNode>();
            NodeChangedArgs received = null;
            dict.RegisterOnNodeChanged(args => received = args);

            dict.Add(42, new SimpleNode());

            Assert.AreEqual("42", received.Changes[0].FieldName);
        }

        [Test]
        public void IDictionaryInterface_Works()
        {
            var dict = new RetreeDictionary<string, SimpleNode>();
            var item = new SimpleNode();
            dict.Add("k", item);

            Assert.AreEqual(1, dict.Count);
            Assert.IsTrue(dict.ContainsKey("k"));
            Assert.IsTrue(dict.TryGetValue("k", out var found));
            Assert.AreSame(item, found);
        }
    }
}
