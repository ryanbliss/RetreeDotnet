// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RetreeCore.Unity
{
    [Serializable]
    public class SerializedRetreeDictionary<TKey, TValue>
        : RetreeDictionary<TKey, TValue>, ISerializationCallbackReceiver
        where TValue : RetreeNode
    {
        [Serializable]
        public struct Pair
        {
            [SerializeField] public TKey key;
            [SerializeField] public TValue value;

            public static implicit operator KeyValuePair<TKey, TValue>(Pair pair)
                => new KeyValuePair<TKey, TValue>(pair.key, pair.value);

            public static implicit operator Pair(KeyValuePair<TKey, TValue> pair)
                => new Pair { key = pair.Key, value = pair.Value };
        }

        [SerializeField] private List<Pair> entries = new List<Pair>();

        [NonSerialized] public bool IsDeserialized = false;
        [NonSerialized] private bool _isDirty = false;

        public bool IsDirty { get => _isDirty; set => _isDirty = value; }

        public Action onAfterDeserialize;

        public SerializedRetreeDictionary()
        {
            IsDeserialized = true;
        }

        public SerializedRetreeDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
        {
            IsDeserialized = true;
        }

        public void OnBeforeSerialize()
        {
            if (IsDirty)
                SyncEntries();

            entries.Clear();
            foreach (var pair in this)
                entries.Add(pair);
        }

        public void OnAfterDeserialize()
        {
            SyncEntries();
            IsDeserialized = true;
            onAfterDeserialize?.Invoke();
        }

        protected void SyncEntries()
        {
            Clear();
            foreach (var entry in entries)
                this[entry.key] = entry.value;
            IsDirty = false;
        }

        public void RegisterDeserializedListener(Action listener)
        {
            if (IsDeserialized)
                listener.Invoke();
            else
                onAfterDeserialize += listener;
        }

        public void UnregisterDeserializedListener(Action listener)
        {
            onAfterDeserialize -= listener;
        }

        public Dictionary<TKey, TValue> ToDictionary()
        {
            var dict = new Dictionary<TKey, TValue>();
            foreach (var entry in entries)
                dict[entry.key] = entry.value;
            return dict;
        }

        public ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary()
            => new ReadOnlyDictionary<TKey, TValue>(ToDictionary());
    }
}
