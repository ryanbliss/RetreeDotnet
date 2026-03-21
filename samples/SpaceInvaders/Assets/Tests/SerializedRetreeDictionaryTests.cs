// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using NUnit.Framework;
using RetreeCore;
using RetreeCore.Unity;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpaceInvaders.Tests
{
    /// <summary>
    /// Simple serializable node used as TValue in SerializedRetreeDictionary tests.
    /// Uses public fields so Retree can detect changes at depth.
    /// </summary>
    [Serializable]
    public class TestValueNode : RetreeNode
    {
        public int value = 0;
        public string label = "";
    }

    /// <summary>
    /// A root RetreeNode that holds a SerializedRetreeDictionary as a field,
    /// used to test depth-change propagation via OnTreeChanged on the parent.
    /// </summary>
    [Serializable]
    public class NodeWithSerializedDict : RetreeNode
    {
        public SerializedRetreeDictionary<string, TestValueNode> dict =
            new SerializedRetreeDictionary<string, TestValueNode>();
    }

    public class SerializedRetreeDictionaryTests
    {
        [TearDown]
        public void TearDown()
        {
            Retree.StopTicks();
        }

        // -------------------------------------------------------------------------
        // Round-trip serialization
        // -------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator RoundTrip_OnBeforeSerialize_ThenOnAfterDeserialize_RestoresEntries()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            var a = new TestValueNode { value = 1 };
            var b = new TestValueNode { value = 2 };
            dict.Add("a", a);
            dict.Add("b", b);

            // Simulate Unity serializing: snapshot entries from _inner
            dict.OnBeforeSerialize();

            // Simulate _inner being wiped (as Unity would after deserialize)
            dict.Clear();
            Assert.AreEqual(0, dict.Count);

            // Simulate Unity deserializing: rebuild _inner from entries
            dict.OnAfterDeserialize();

            Assert.AreEqual(2, dict.Count);
            Assert.IsTrue(dict.ContainsKey("a"));
            Assert.IsTrue(dict.ContainsKey("b"));
            Assert.AreEqual(1, dict["a"].value);
            Assert.AreEqual(2, dict["b"].value);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RoundTrip_Remove_ItemAbsentAfterDeserialize()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            dict.Add("keep", new TestValueNode { value = 10 });
            dict.Add("remove", new TestValueNode { value = 99 });

            dict.Remove("remove");

            dict.OnBeforeSerialize();
            dict.Clear();
            dict.OnAfterDeserialize();

            Assert.AreEqual(1, dict.Count);
            Assert.IsTrue(dict.ContainsKey("keep"));
            Assert.IsFalse(dict.ContainsKey("remove"));
            yield return null;
        }

        // -------------------------------------------------------------------------
        // Retree events fired during SyncEntries
        // -------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator OnAfterDeserialize_FiresRetreeNodeChangedEvents()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            dict.Add("x", new TestValueNode { value = 7 });
            dict.OnBeforeSerialize();
            dict.Clear();

            NodeChangedArgs received = null;
            dict.OnNodeChanged(args => received = args);

            dict.OnAfterDeserialize();

            Assert.IsNotNull(received, "SyncEntries should fire OnNodeChanged for each restored entry");
            Assert.AreEqual("x", received.Changes[0].FieldName);

            Retree.ClearListeners(dict, recursive: true);
            yield return null;
        }

        // -------------------------------------------------------------------------
        // IsDirty
        // -------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator IsDirty_ResetToFalseAfterOnBeforeSerialize()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            dict.Add("k", new TestValueNode());
            dict.IsDirty = true;

            dict.OnBeforeSerialize();

            Assert.IsFalse(dict.IsDirty);
            yield return null;
        }

        // -------------------------------------------------------------------------
        // IsDeserialized and onAfterDeserialize callback
        // -------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator IsDeserialized_TrueAfterConstructor()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            Assert.IsTrue(dict.IsDeserialized);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnAfterDeserialize_SetsIsDeserializedTrue()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            dict.IsDeserialized = false;

            dict.OnAfterDeserialize();

            Assert.IsTrue(dict.IsDeserialized);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnAfterDeserialize_InvokesOnAfterDeserializeCallback()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            bool callbackFired = false;
            dict.onAfterDeserialize += () => callbackFired = true;

            dict.OnAfterDeserialize();

            Assert.IsTrue(callbackFired);
            yield return null;
        }

        // -------------------------------------------------------------------------
        // RegisterDeserializedListener / UnregisterDeserializedListener
        // -------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator RegisterDeserializedListener_WhenAlreadyDeserialized_InvokesImmediately()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            Assert.IsTrue(dict.IsDeserialized);

            bool invoked = false;
            dict.RegisterDeserializedListener(() => invoked = true);

            Assert.IsTrue(invoked, "Listener should be invoked immediately when already deserialized");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RegisterDeserializedListener_WhenNotYetDeserialized_InvokesAfterDeserialize()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            dict.IsDeserialized = false;

            bool invoked = false;
            dict.RegisterDeserializedListener(() => invoked = true);

            Assert.IsFalse(invoked, "Listener should not be invoked before deserialize");

            dict.OnAfterDeserialize();

            Assert.IsTrue(invoked, "Listener should be invoked after OnAfterDeserialize");
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnregisterDeserializedListener_PreventsInvocation()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            dict.IsDeserialized = false;

            bool invoked = false;
            void Listener() => invoked = true;
            dict.RegisterDeserializedListener(Listener);
            dict.UnregisterDeserializedListener(Listener);

            dict.OnAfterDeserialize();

            Assert.IsFalse(invoked, "Unregistered listener should not be invoked");
            yield return null;
        }

        // -------------------------------------------------------------------------
        // ToDictionary / ToReadOnlyDictionary
        // -------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ToDictionary_ReturnsEntriesSnapshot()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            var node = new TestValueNode { value = 42 };
            dict.Add("k", node);
            dict.OnBeforeSerialize(); // populate entries list

            var snapshot = dict.ToDictionary();

            Assert.AreEqual(1, snapshot.Count);
            Assert.IsTrue(snapshot.ContainsKey("k"));
            Assert.AreEqual(42, snapshot["k"].value);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ToReadOnlyDictionary_ReturnsEntriesSnapshot()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            dict.Add("r", new TestValueNode { value = 5 });
            dict.OnBeforeSerialize();

            var ro = dict.ToReadOnlyDictionary();

            Assert.AreEqual(1, ro.Count);
            Assert.AreEqual(5, ro["r"].value);
            yield return null;
        }

        // -------------------------------------------------------------------------
        // Depth changes — public field on TValue node propagates via OnTreeChanged.
        // NOTE: Retree tracks public *fields* only, not properties.
        // -------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator DepthChange_MutatingValueNodeField_FiresOnTreeChanged()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            var node = new TestValueNode { value = 0 };
            dict.Add("node", node);

            TreeChangedArgs received = null;
            dict.OnTreeChanged(args => received = args);

            node.value = 99;
            Retree.Tick();

            Assert.IsNotNull(received, "Mutating a value node's public field should fire OnTreeChanged on the dict");
            Assert.AreSame(dict, received.ListenerNode);
            Assert.AreSame(node, received.SourceNode);

            Retree.ClearListeners(dict, recursive: true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DepthChange_AfterRoundTrip_RestoredNodeStillTracked()
        {
            var dict = new SerializedRetreeDictionary<string, TestValueNode>();
            var node = new TestValueNode { value = 0 };
            dict.Add("node", node);

            TreeChangedArgs received = null;
            dict.OnTreeChanged(args => received = args);

            // Simulate serialization round-trip
            dict.OnBeforeSerialize();
            dict.Clear();
            dict.OnAfterDeserialize();

            // The restored node instance should be tracked
            var restoredNode = dict["node"];
            restoredNode.value = 55;
            Retree.Tick();

            Assert.IsNotNull(received, "Depth changes on restored node should propagate after round-trip");
            Assert.AreEqual(55, restoredNode.value);

            Retree.ClearListeners(dict, recursive: true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DepthChange_ParentNodeWithSerializedDict_FiresOnTreeChanged()
        {
            var parent = new NodeWithSerializedDict();
            var node = new TestValueNode { value = 0 };
            parent.dict.Add("child", node);

            TreeChangedArgs received = null;
            parent.OnTreeChanged(args => received = args);
            Retree.Tick();

            node.value = 7;
            Retree.Tick();

            Assert.IsNotNull(received, "Depth change inside SerializedRetreeDictionary should propagate to parent node");

            Retree.ClearListeners(parent, recursive: true);
            yield return null;
        }
    }
}
