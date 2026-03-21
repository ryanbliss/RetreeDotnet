// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using RetreeCore.Internal;

namespace RetreeCore
{
    public abstract class RetreeNode : RetreeBase
    {
        private FieldMetadata[] _fields;
        private object[] _fieldSnapshot;
        private bool _isListeningToNode;
        internal bool _isListeningToTree;

        // Tracks internal OnNodeChanged listeners registered on child nodes/collections
        private readonly Dictionary<RetreeBase, Action<NodeChangedArgs>> _childListeners =
            new Dictionary<RetreeBase, Action<NodeChangedArgs>>();

        // Tracks internal OnTreeChanged listeners registered on child collections
        // (needed so item field changes propagate through collections to this node)
        private readonly Dictionary<RetreeBase, Action<TreeChangedArgs>> _childTreeListeners =
            new Dictionary<RetreeBase, Action<TreeChangedArgs>>();

        protected override void OnFirstNodeListenerAdded()
        {
            if (!_isListeningToNode)
                ListenToNodeChanges();
        }

        protected override void OnLastNodeListenerRemoved()
        {
            if (!_isListeningToTree)
                StopListeningToNodeChanges();
        }

        protected override void OnFirstTreeListenerAdded()
        {
            ListenToTreeChanges();
        }

        protected override void OnLastTreeListenerRemoved()
        {
            StopListeningToTreeChanges();
            if (!HasNodeListeners)
                StopListeningToNodeChanges();
        }

        private void ListenToNodeChanges()
        {
            if (_isListeningToNode) return;
            _isListeningToNode = true;

            _fields = FieldCache.GetFields(GetType());
            TakeSnapshot();
            RetreeRegistry.Register(this);
        }

        private void TakeSnapshot()
        {
            _fieldSnapshot = new object[_fields.Length];
            for (int i = 0; i < _fields.Length; i++)
            {
                var value = _fields[i].Getter(this);
                _fieldSnapshot[i] = value;

                // Set parent for any child nodes/collections discovered during snapshot
                if (value is RetreeBase child && child._parent == null)
                    child._parent = this;
            }
        }

        private void StopListeningToNodeChanges()
        {
            if (!_isListeningToNode) return;
            _isListeningToNode = false;

            RetreeRegistry.Unregister(this);
            _fieldSnapshot = null;
        }

        private void ListenToTreeChanges()
        {
            if (_isListeningToTree) return;
            _isListeningToTree = true;

            // Ensure node listening is active (needed for polling)
            if (!_isListeningToNode)
                ListenToNodeChanges();

            // Subscribe to all current child nodes and collections
            SubscribeToChildren();
        }

        private void StopListeningToTreeChanges()
        {
            if (!_isListeningToTree) return;
            _isListeningToTree = false;

            UnsubscribeFromAllChildren();
        }

        private void SubscribeToChildren()
        {
            if (_fields == null) return;

            for (int i = 0; i < _fields.Length; i++)
            {
                var field = _fields[i];
                if (field.Kind == FieldKind.Value) continue;

                var value = _fieldSnapshot[i] as RetreeBase;
                if (value != null)
                    SubscribeToChild(value);
            }
        }

        private void SubscribeToChild(RetreeBase child)
        {
            if (_childListeners.ContainsKey(child)) return;

            // Subscribe to child's OnNodeChanged (handles direct mutations + field changes)
            Action<NodeChangedArgs> listener = args => OnChildNodeChanged(child, args);
            _childListeners[child] = listener;
            child.OnNodeChanged(listener);

            if (child is RetreeNode childNode)
            {
                // RetreeNode child: recursively ensure it's listening + subscribed
                if (!childNode._isListeningToTree)
                {
                    if (!childNode._isListeningToNode)
                        childNode.ListenToNodeChanges();
                    childNode.SubscribeToChildren();
                    childNode._isListeningToTree = true;
                }
            }
            else
            {
                // Collection child (RetreeList/RetreeDictionary): subscribe to OnTreeChanged
                // so item field changes propagate through. This also triggers the collection's
                // OnFirstTreeListenerAdded which subscribes to each item's OnNodeChanged.
                Action<TreeChangedArgs> treeListener = args => OnChildCollectionTreeChanged(args);
                _childTreeListeners[child] = treeListener;
                child.OnTreeChanged(treeListener);
            }
        }

        private void UnsubscribeFromChild(RetreeBase child)
        {
            if (!_childListeners.TryGetValue(child, out var listener)) return;

            child.OffNodeChanged(listener);
            _childListeners.Remove(child);

            if (child is RetreeNode childNode)
            {
                // RetreeNode child: recursively clean up
                if (childNode._isListeningToTree)
                {
                    childNode.UnsubscribeFromAllChildren();
                    childNode._isListeningToTree = false;
                    if (!childNode.HasNodeListeners)
                        childNode.StopListeningToNodeChanges();
                }
            }
            else if (_childTreeListeners.TryGetValue(child, out var treeListener))
            {
                // Collection child: also unsubscribe from tree changes
                child.OffTreeChanged(treeListener);
                _childTreeListeners.Remove(child);
            }
        }

        private void UnsubscribeFromAllChildren()
        {
            var children = new List<RetreeBase>(_childListeners.Keys);
            foreach (var child in children)
            {
                UnsubscribeFromChild(child);
            }
        }

        /// <summary>
        /// Called when a child node or collection fires OnNodeChanged.
        /// Updates subscriptions for reference changes and propagates upward as tree change.
        /// </summary>
        private void OnChildNodeChanged(RetreeBase child, NodeChangedArgs args)
        {
            // Update subscriptions for node reference changes in the child's args
            foreach (var change in args.Changes)
            {
                if (change.OldValue is RetreeBase oldChild)
                    UnsubscribeFromChild(oldChild);
                if (change.NewValue is RetreeBase newChild)
                    SubscribeToChild(newChild);
            }

            // Propagate upward as tree change using the shared propagation method.
            // This fires OnTreeChanged on self (if has tree listeners) + all ancestors.
            PropagateAsTreeChange(args.Node, args.Changes);
        }

        /// <summary>
        /// Called when a child collection (RetreeList/RetreeDictionary) fires OnTreeChanged.
        /// This happens when an item within the collection changes its own fields.
        /// We propagate this upward starting from ourselves.
        /// Note: we do NOT call PropagateAsTreeChange here because the collection's
        /// PropagateAsTreeChange already walks up through us via the _parent chain.
        /// This handler is only here to receive the tree change — propagation is handled.
        /// </summary>
        private void OnChildCollectionTreeChanged(TreeChangedArgs args)
        {
            // The collection's PropagateAsTreeChange already fires on us and our ancestors.
            // This callback exists solely to keep the subscription alive (needed for the
            // collection's OnFirstTreeListenerAdded to fire). No action needed here.
        }

        /// <summary>
        /// Enables deep (tree) tracking on this node without adding an external tree listener.
        /// Used by RetreeList and RetreeDictionary to recursively track items' child nodes.
        /// Sets _isListeningToTree = true and calls SubscribeToChildren(), but does NOT
        /// increment _treeListenerCount, so EmitTreeChanged is NOT invoked on this node
        /// during PropagateAsTreeChange — propagation still walks up via _parent.
        /// </summary>
        internal void BeginDeepTracking()
        {
            if (_isListeningToTree) return;

            if (!_isListeningToNode)
                ListenToNodeChanges();

            _isListeningToTree = true;
            SubscribeToChildren();
        }

        /// <summary>
        /// Stops deep tracking started by BeginDeepTracking(), if no external tree listener
        /// is keeping it alive.
        /// </summary>
        internal void EndDeepTracking()
        {
            if (!_isListeningToTree) return;
            if (HasTreeListeners) return; // real external listener keeps it alive

            _isListeningToTree = false;
            UnsubscribeFromAllChildren();

            if (!HasNodeListeners)
                StopListeningToNodeChanges();
        }

        /// <summary>
        /// Called during Retree.Tick(). Compares current field values to snapshot.
        /// </summary>
        internal void PollForChanges()
        {
            if (_fields == null || _fieldSnapshot == null) return;

            List<FieldChange> changes = null;

            for (int i = 0; i < _fields.Length; i++)
            {
                var field = _fields[i];
                var currentValue = field.Getter(this);
                var previousValue = _fieldSnapshot[i];

                bool changed;
                if (field.IsReferenceComparison)
                    changed = !ReferenceEquals(currentValue, previousValue);
                else
                    changed = !Equals(currentValue, previousValue);

                if (changed)
                {
                    if (changes == null)
                        changes = new List<FieldChange>();
                    changes.Add(new FieldChange(field.Name, previousValue, currentValue));
                    _fieldSnapshot[i] = currentValue;

                    // Handle parent tracking for node/collection reference changes
                    if (field.Kind != FieldKind.Value)
                    {
                        if (previousValue is RetreeBase oldChild && ReferenceEquals(oldChild._parent, this))
                            oldChild._parent = null;
                        if (currentValue is RetreeBase newChild)
                        {
                            if (newChild._parent != null && !ReferenceEquals(newChild._parent, this))
                                throw new InvalidOperationException(
                                    $"Node already belongs to another parent. Remove it first. Field: {field.Name}");
                            newChild._parent = this;
                        }
                    }
                }
            }

            if (changes == null) return;

            var args = new NodeChangedArgs(this, changes);

            if (Retree.IsSilent)
                return;

            // 1) Fire OnNodeChanged. Parent's OnChildNodeChanged subscription handles
            //    upward tree propagation — we must NOT also propagate from here.
            if (Retree.InTransaction)
                Retree.QueueNodeChange(this, changes);
            else
                EmitNodeChanged(args);

            // 2) Handle self tree change only (update own subscriptions + emit self tree changed).
            //    No upward propagation — that's done by the subscription chain above.
            if (_isListeningToTree)
            {
                foreach (var change in changes)
                {
                    if (change.OldValue is RetreeBase oldChild)
                        UnsubscribeFromChild(oldChild);
                    if (change.NewValue is RetreeBase newChild)
                        SubscribeToChild(newChild);
                }

                // Only emit if there are real external listeners on this node.
                // If _isListeningToTree is set via BeginDeepTracking (no real listener),
                // HasTreeListeners = false and we skip the allocation entirely.
                if (HasTreeListeners)
                {
                    if (Retree.InTransaction)
                        Retree.QueueTreeChange(this, this, changes);
                    else
                        EmitTreeChanged(new TreeChangedArgs(this, this, changes));
                }
            }
        }
    }
}
