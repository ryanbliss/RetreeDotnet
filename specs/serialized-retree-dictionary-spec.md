# SerializedRetreeDictionary Spec

## Goal

Add a Unity-serializable dictionary to the Retree Unity package that bridges Unity's `ISerializationCallbackReceiver` serialization lifecycle with `RetreeDictionary`'s change notification system.

## Core design decisions

- **Inheritance over composition**: `SerializedRetreeDictionary<TKey, TValue>` extends `RetreeDictionary<TKey, TValue>` and implements `ISerializationCallbackReceiver`. This requires unsealing `RetreeDictionary` in the core library.
- **`TValue : RetreeNode`**: Inherits the same constraint from `RetreeDictionary`.
- **No `TKey` constraint**: Users are responsible for choosing key types that Unity can serialize. Unity will fail serialization naturally if the key type is unsupported.
- **Retree events fire normally during `SyncEntries`**: No silent suppression. Restoring from serialized state fires change events like any other mutation.
- **No custom `OnChange`/keyed listeners**: Retree's existing `OnNodeChanged`/`OnTreeChanged` system handles change notification. These are not duplicated here.

## Required changes

### 1. Core library — unseal `RetreeDictionary`

In `src/RetreeCore/RetreeDictionary.cs`, remove `sealed` from the class declaration:

```csharp
// Before
public sealed class RetreeDictionary<TKey, TValue> : RetreeBase, ...

// After
public class RetreeDictionary<TKey, TValue> : RetreeBase, ...
```

No other changes to core.

### 2. New file — `src/RetreeUnity/Runtime/SerializedRetreeDictionary.cs`

**Namespace**: `RetreeCore.Unity`

**Class declaration**:
```csharp
[Serializable]
public class SerializedRetreeDictionary<TKey, TValue>
    : RetreeDictionary<TKey, TValue>, ISerializationCallbackReceiver
    where TValue : RetreeNode
```

#### Nested `Pair` struct

A `[Serializable]` struct to hold one key-value entry for Unity's serialized list:

```csharp
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
```

#### Serialized backing field

```csharp
[SerializeField] private List<Pair> entries = new List<Pair>();
```

#### Deserialization state

```csharp
[NonSerialized] public bool IsDeserialized = false;
[NonSerialized] private bool _isDirty = false;

public bool IsDirty { get => _isDirty; set => _isDirty = value; }

public Action onAfterDeserialize;
```

#### Constructors

```csharp
public SerializedRetreeDictionary()
{
    IsDeserialized = true;
}

public SerializedRetreeDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
{
    IsDeserialized = true;
}
```

> **Note**: `RetreeDictionary` currently has no constructor accepting `IDictionary`. That constructor should be added to `RetreeDictionary` when implementing this feature, or the second constructor above can be omitted if it's out of scope. Confirm with the implementer.

#### `ISerializationCallbackReceiver` implementation

```csharp
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
```

#### `SyncEntries`

Rebuilds the dictionary from `entries`. Fires Retree change events normally (no `Retree.Silent` wrapping).

```csharp
protected void SyncEntries()
{
    Clear();
    foreach (var entry in entries)
        this[entry.key] = entry.value;
    IsDirty = false;
}
```

#### Utility methods

```csharp
public Dictionary<TKey, TValue> ToDictionary()
{
    var dict = new Dictionary<TKey, TValue>();
    foreach (var entry in entries)
        dict[entry.key] = entry.value;
    return dict;
}

public ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary()
    => new ReadOnlyDictionary<TKey, TValue>(ToDictionary());
```

#### Deserialization listener helpers

Mirrors the pattern from the source reference for consumers that need to act after deserialization is complete:

```csharp
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
```

## What is intentionally omitted

| Source feature | Reason omitted |
|---|---|
| `OnChange` action | Retree's `OnNodeChanged`/`OnTreeChanged` covers this |
| `_listeners` (per-key action subscriptions) | Same — Retree handles change propagation |
| `Set(key, value)` with manual event dispatch | Use `this[key] = value` via `RetreeDictionary` |
| `NotifyKeyedChange` | No longer needed without per-key listeners |
| `IHasDirty` interface | Can be added later if needed across multiple types; out of scope here |
| `[DoNotSerialize]` on private fields | Unity doesn't serialize private fields by default; attribute not needed |

## File locations summary

| File | Change |
|---|---|
| `src/RetreeCore/RetreeDictionary.cs` | Remove `sealed` |
| `src/RetreeUnity/Runtime/SerializedRetreeDictionary.cs` | New file |

## Open question for implementer

Does `RetreeDictionary` need a constructor that accepts `IDictionary<TKey, TValue>` to support the second `SerializedRetreeDictionary` constructor? If the use case for pre-populating from an existing dictionary is needed, add it. Otherwise, omit the second constructor.
