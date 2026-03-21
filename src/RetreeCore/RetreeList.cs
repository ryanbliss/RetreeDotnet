// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

namespace RetreeCore
{
    public sealed class RetreeList<T> : RetreeBase, IList<T>, IReadOnlyList<T> where T : RetreeNode
    {
        private readonly List<T> _inner = new List<T>();

        // Tracks internal listeners on items for tree propagation
        private readonly Dictionary<T, Action<NodeChangedArgs>> _itemListeners =
            new Dictionary<T, Action<NodeChangedArgs>>();

        public int Count => _inner.Count;
        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => _inner[index];
            set
            {
                var old = _inner[index];
                if (ReferenceEquals(old, value)) return;

                if (old != null)
                {
                    if (ReferenceEquals(old._parent, this))
                        old._parent = null;
                    UnsubscribeFromItem(old);
                }

                if (value != null)
                {
                    ValidateParent(value);
                    value._parent = this;
                }

                _inner[index] = value;
                EmitListChange(old, value);

                if (value != null && HasTreeListeners)
                    SubscribeToItem(value);
            }
        }

        public void Add(T item)
        {
            if (item != null)
            {
                ValidateParent(item);
                item._parent = this;
            }

            _inner.Add(item);
            EmitListChange(null, item);

            if (item != null && HasTreeListeners)
                SubscribeToItem(item);
        }

        public void Insert(int index, T item)
        {
            if (item != null)
            {
                ValidateParent(item);
                item._parent = this;
            }

            _inner.Insert(index, item);
            EmitListChange(null, item);

            if (item != null && HasTreeListeners)
                SubscribeToItem(item);
        }

        public bool Remove(T item)
        {
            var removed = _inner.Remove(item);
            if (!removed) return false;

            if (item != null)
            {
                if (ReferenceEquals(item._parent, this))
                    item._parent = null;
                UnsubscribeFromItem(item);
            }

            EmitListChange(item, null);
            return true;
        }

        public void RemoveAt(int index)
        {
            var item = _inner[index];
            _inner.RemoveAt(index);

            if (item != null)
            {
                if (ReferenceEquals(item._parent, this))
                    item._parent = null;
                UnsubscribeFromItem(item);
            }

            EmitListChange(item, null);
        }

        public void Clear()
        {
            if (_inner.Count == 0) return;

            var changes = new List<FieldChange>();
            foreach (var item in _inner)
            {
                if (item != null)
                {
                    if (ReferenceEquals(item._parent, this))
                        item._parent = null;
                    UnsubscribeFromItem(item);
                    changes.Add(new FieldChange("Items", item, null));
                }
            }

            _inner.Clear();

            if (changes.Count > 0)
                EmitListChanges(changes);
        }

        public bool Contains(T item) => _inner.Contains(item);
        public int IndexOf(T item) => _inner.IndexOf(item);
        public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        protected override void OnFirstTreeListenerAdded()
        {
            foreach (var item in _inner)
            {
                if (item != null)
                    SubscribeToItem(item);
            }
        }

        protected override void OnLastTreeListenerRemoved()
        {
            var items = new List<T>(_itemListeners.Keys);
            foreach (var item in items)
                UnsubscribeFromItem(item);
        }

        private void SubscribeToItem(T item)
        {
            if (_itemListeners.ContainsKey(item)) return;

            Action<NodeChangedArgs> listener = args => OnItemNodeChanged(item, args);
            _itemListeners[item] = listener;
            item.OnNodeChanged(listener);

            // Enable deep tracking so child RetreeNode fields inside this item are polled.
            // BeginDeepTracking sets _isListeningToTree without adding a delegate, so
            // EmitTreeChanged is NOT invoked on the item — propagation walks up via _parent.
            item.BeginDeepTracking();
        }

        private void UnsubscribeFromItem(T item)
        {
            if (!_itemListeners.TryGetValue(item, out var listener)) return;
            item.OffNodeChanged(listener);
            _itemListeners.Remove(item);

            item.EndDeepTracking();
        }

        /// <summary>
        /// Called when an item within this list fires OnNodeChanged.
        /// Propagates upward as a tree change using the shared mechanism.
        /// </summary>
        private void OnItemNodeChanged(T item, NodeChangedArgs args)
        {
            // Use the shared upward propagation — fires on self (if tree listeners) + all ancestors
            PropagateAsTreeChange(args.Node, args.Changes);
        }

        /// <summary>
        /// Fires OnNodeChanged for a single-item list mutation (add, remove, replace).
        /// Parent catches this via its subscription to our OnNodeChanged.
        /// </summary>
        private void EmitListChange(T oldItem, T newItem)
        {
            if (!HasAnyListeners) return;
            if (Retree.IsSilent) return;

            var changes = new List<FieldChange> { new FieldChange("Items", oldItem, newItem) };
            var args = new NodeChangedArgs(this, changes);

            if (Retree.InTransaction)
                Retree.QueueNodeChange(this, changes);
            else
                EmitNodeChanged(args);
        }

        /// <summary>
        /// Fires OnNodeChanged for bulk list mutations (Clear).
        /// </summary>
        private void EmitListChanges(List<FieldChange> changes)
        {
            if (!HasAnyListeners) return;
            if (Retree.IsSilent) return;

            var args = new NodeChangedArgs(this, changes);

            if (Retree.InTransaction)
                Retree.QueueNodeChange(this, changes);
            else
                EmitNodeChanged(args);
        }

        private static void ValidateParent(T item)
        {
            if (item._parent != null)
                throw new InvalidOperationException(
                    "Node already belongs to another parent. Remove it first.");
        }
    }
}
