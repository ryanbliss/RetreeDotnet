// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

namespace RetreeCore
{
    public class RetreeDictionary<TKey, TValue> : RetreeBase, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
        where TValue : RetreeNode
    {
        private readonly Dictionary<TKey, TValue> _inner = new Dictionary<TKey, TValue>();

        public RetreeDictionary() { }

        public RetreeDictionary(IDictionary<TKey, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
                Add(kvp.Key, kvp.Value);
        }

        private readonly Dictionary<TValue, Action<NodeChangedArgs>> _valueListeners =
            new Dictionary<TValue, Action<NodeChangedArgs>>();

        public int Count => _inner.Count;
        public bool IsReadOnly => false;
        public ICollection<TKey> Keys => _inner.Keys;
        public ICollection<TValue> Values => _inner.Values;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _inner.Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _inner.Values;

        public TValue this[TKey key]
        {
            get => _inner[key];
            set
            {
                var hasExisting = _inner.TryGetValue(key, out var old);
                if (hasExisting && ReferenceEquals(old, value)) return;

                if (hasExisting && old != null)
                {
                    if (ReferenceEquals(old._parent, this))
                        old._parent = null;
                    UnsubscribeFromValue(old);
                }

                if (value != null)
                {
                    ValidateParent(value);
                    value._parent = this;
                }

                _inner[key] = value;
                EmitDictChange(key, old, value);

                if (value != null && HasTreeListeners)
                    SubscribeToValue(value);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (value != null)
            {
                ValidateParent(value);
                value._parent = this;
            }

            _inner.Add(key, value);
            EmitDictChange(key, null, value);

            if (value != null && HasTreeListeners)
                SubscribeToValue(value);
        }

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public bool Remove(TKey key)
        {
            if (!_inner.TryGetValue(key, out var value))
                return false;

            _inner.Remove(key);

            if (value != null)
            {
                if (ReferenceEquals(value._parent, this))
                    value._parent = null;
                UnsubscribeFromValue(value);
            }

            EmitDictChange(key, value, null);
            return true;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!_inner.TryGetValue(item.Key, out var value)) return false;
            if (!EqualityComparer<TValue>.Default.Equals(value, item.Value)) return false;
            return Remove(item.Key);
        }

        public void Clear()
        {
            if (_inner.Count == 0) return;

            var changes = new List<FieldChange>();
            foreach (var kvp in _inner)
            {
                if (kvp.Value != null)
                {
                    if (ReferenceEquals(kvp.Value._parent, this))
                        kvp.Value._parent = null;
                    UnsubscribeFromValue(kvp.Value);
                    changes.Add(new FieldChange(kvp.Key?.ToString() ?? "null", kvp.Value, null));
                }
            }

            _inner.Clear();

            if (changes.Count > 0)
            {
                if (Retree.IsSilent) return;

                var args = new NodeChangedArgs(this, changes);
                if (Retree.InTransaction)
                    Retree.QueueNodeChange(this, changes);
                else
                    EmitNodeChanged(args);
            }
        }

        public bool ContainsKey(TKey key) => _inner.ContainsKey(key);
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Contains(item);
        public bool TryGetValue(TKey key, out TValue value) => _inner.TryGetValue(key, out value);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        protected override void OnFirstTreeListenerAdded()
        {
            foreach (var kvp in _inner)
            {
                if (kvp.Value != null)
                    SubscribeToValue(kvp.Value);
            }
        }

        protected override void OnLastTreeListenerRemoved()
        {
            var values = new List<TValue>(_valueListeners.Keys);
            foreach (var value in values)
                UnsubscribeFromValue(value);
        }

        private void SubscribeToValue(TValue value)
        {
            if (_valueListeners.ContainsKey(value)) return;

            Action<NodeChangedArgs> listener = args => OnValueNodeChanged(value, args);
            _valueListeners[value] = listener;
            value.OnNodeChanged(listener);
        }

        private void UnsubscribeFromValue(TValue value)
        {
            if (!_valueListeners.TryGetValue(value, out var listener)) return;
            value.OffNodeChanged(listener);
            _valueListeners.Remove(value);
        }

        /// <summary>
        /// Called when a value in this dictionary fires OnNodeChanged.
        /// Propagates upward as a tree change using the shared mechanism.
        /// </summary>
        private void OnValueNodeChanged(TValue value, NodeChangedArgs args)
        {
            PropagateAsTreeChange(args.Node, args.Changes);
        }

        /// <summary>
        /// Fires OnNodeChanged for a single-entry mutation (add, remove, replace).
        /// Parent catches this via its subscription to our OnNodeChanged.
        /// </summary>
        private void EmitDictChange(TKey key, TValue oldValue, TValue newValue)
        {
            if (!HasAnyListeners) return;
            if (Retree.IsSilent) return;

            var fieldName = key?.ToString() ?? "null";
            var changes = new List<FieldChange> { new FieldChange(fieldName, oldValue, newValue) };
            var args = new NodeChangedArgs(this, changes);

            if (Retree.InTransaction)
                Retree.QueueNodeChange(this, changes);
            else
                EmitNodeChanged(args);
        }

        private static void ValidateParent(TValue value)
        {
            if (value._parent != null)
                throw new InvalidOperationException(
                    "Node already belongs to another parent. Remove it first.");
        }
    }
}
