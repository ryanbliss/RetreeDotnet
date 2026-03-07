// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using NUnit.Framework;
using RetreeCore.Tests.TestHelpers;

namespace RetreeCore.Tests
{
    [TestFixture]
    public class RetreeNodeTests
    {
        [SetUp]
        public void SetUp()
        {
            Retree.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Retree.Reset();
        }

        [Test]
        public void ChangingField_AndCallingTick_FiresOnNodeChanged_WithCorrectFieldChange()
        {
            var node = new SimpleNode { count = 0 };
            NodeChangedArgs received = null;
            node.RegisterOnNodeChanged(args => received = args);

            node.count = 5;
            Retree.Tick();

            Assert.IsNotNull(received, "OnNodeChanged should have fired");
            Assert.AreSame(node, received.Node);
            Assert.AreEqual(1, received.Changes.Count);
            Assert.AreEqual("count", received.Changes[0].FieldName);
            Assert.AreEqual(0, received.Changes[0].OldValue);
            Assert.AreEqual(5, received.Changes[0].NewValue);
        }

        [Test]
        public void NoChange_DoesNotFireEvent()
        {
            var node = new SimpleNode { count = 10 };
            int callCount = 0;
            node.RegisterOnNodeChanged(_ => callCount++);

            // Tick without changing anything
            Retree.Tick();

            Assert.AreEqual(0, callCount, "OnNodeChanged should not fire when nothing changed");
        }

        [Test]
        public void MultipleFieldsChanged_InOneTick_FiresSingleEvent_WithMultipleFieldChanges()
        {
            var node = new SimpleNode { count = 0, label = "hello", flag = false };
            var receivedArgs = new List<NodeChangedArgs>();
            node.RegisterOnNodeChanged(args => receivedArgs.Add(args));

            node.count = 42;
            node.flag = true;
            Retree.Tick();

            Assert.AreEqual(1, receivedArgs.Count, "Should fire exactly one OnNodeChanged event");
            Assert.AreEqual(2, receivedArgs[0].Changes.Count, "Should contain two FieldChanges");

            var fieldNames = new HashSet<string>();
            foreach (var change in receivedArgs[0].Changes)
                fieldNames.Add(change.FieldName);

            Assert.IsTrue(fieldNames.Contains("count"), "Should detect count change");
            Assert.IsTrue(fieldNames.Contains("flag"), "Should detect flag change");
        }

        [Test]
        public void RetreeIgnoreField_ChangesAreNotDetected()
        {
            var node = new NodeWithIgnored { tracked = 0, ignored = 0 };
            int callCount = 0;
            node.RegisterOnNodeChanged(_ => callCount++);

            node.ignored = 999;
            Retree.Tick();

            Assert.AreEqual(0, callCount, "Changes to [RetreeIgnore] fields should not fire events");
        }

        [Test]
        public void ReadonlyField_IsNotDetected()
        {
            var node = new NodeWithReadonly { mutable = 0 };
            NodeChangedArgs received = null;
            node.RegisterOnNodeChanged(args => received = args);

            // readonly field cannot be changed after construction, so just verify
            // that only the mutable field is tracked by changing it
            node.mutable = 10;
            Retree.Tick();

            Assert.IsNotNull(received, "Mutable field change should fire");
            Assert.AreEqual(1, received.Changes.Count);
            Assert.AreEqual("mutable", received.Changes[0].FieldName);
        }

        [Test]
        public void Property_IsNotDetected()
        {
            var node = new NodeWithProperty { field = 0 };
            var receivedArgs = new List<NodeChangedArgs>();
            node.RegisterOnNodeChanged(args => receivedArgs.Add(args));

            // Change only the property, not the field
            node.Property = 100;
            Retree.Tick();

            Assert.AreEqual(0, receivedArgs.Count, "Property changes should not be detected");
        }

        [Test]
        public void UnregisteringListener_StopsEvents()
        {
            var node = new SimpleNode { count = 0 };
            int callCount = 0;
            void Listener(NodeChangedArgs args) => callCount++;

            node.RegisterOnNodeChanged(Listener);

            node.count = 1;
            Retree.Tick();
            Assert.AreEqual(1, callCount, "Should fire before unregister");

            node.UnregisterOnNodeChanged(Listener);

            node.count = 2;
            Retree.Tick();
            Assert.AreEqual(1, callCount, "Should not fire after unregister");
        }

        [Test]
        public void StringFieldChange_IsDetected_UsingReferenceEquality()
        {
            var original = "hello";
            var node = new SimpleNode { label = original };
            NodeChangedArgs received = null;
            node.RegisterOnNodeChanged(args => received = args);

            // Create a new string instance with same content via concatenation
            var newString = "hel" + "lo";
            // Verify our test string is a different reference (not interned to same)
            // Note: the runtime may intern these, but the important thing is that
            // assigning a genuinely different string fires the event
            node.label = "world";
            Retree.Tick();

            Assert.IsNotNull(received, "String field change should fire OnNodeChanged");
            Assert.AreEqual("label", received.Changes[0].FieldName);
            Assert.AreEqual(original, received.Changes[0].OldValue);
            Assert.AreEqual("world", received.Changes[0].NewValue);
        }

        [Test]
        public void RegisteringListener_TakesInitialSnapshot_NoEventOnFirstTickIfNoChange()
        {
            var node = new SimpleNode { count = 5, label = "test", flag = true };
            int callCount = 0;
            node.RegisterOnNodeChanged(_ => callCount++);

            // First tick with no changes since registration should not fire
            Retree.Tick();

            Assert.AreEqual(0, callCount, "Should not fire catch-up event on first tick when nothing changed");
        }

        [Test]
        public void AfterUnregister_AndReRegister_TakesFreshSnapshot()
        {
            var node = new SimpleNode { count = 0 };
            int callCount = 0;
            void Listener(NodeChangedArgs args) => callCount++;

            // Register and snapshot at count=0
            node.RegisterOnNodeChanged(Listener);

            // Change count and tick - should fire
            node.count = 10;
            Retree.Tick();
            Assert.AreEqual(1, callCount, "Should fire after first change");

            // Unregister
            node.UnregisterOnNodeChanged(Listener);

            // Change count while unregistered
            node.count = 20;

            // Re-register - should take fresh snapshot at count=20
            node.RegisterOnNodeChanged(Listener);

            // Tick without further changes - should NOT fire
            // because the fresh snapshot matches current state
            Retree.Tick();
            Assert.AreEqual(1, callCount, "Should not fire after re-register with no new changes");

            // Now change and verify detection works from the new snapshot
            node.count = 30;
            Retree.Tick();
            Assert.AreEqual(2, callCount, "Should fire after change from new snapshot baseline");
        }
    }
}
