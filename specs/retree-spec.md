# Retree C# spec

## User's base requirements (do not edit)

Retree C# is based on the TypeScript library [Retree](https://github.com/ryanbliss/retree). It has two parts: a core library and a React extension. We will be focusing on porting the core package to C#, which will be used in a Unity project. 

Here are the basics:

```ts
import { Retree } from "@retreejs/core";
import { v4 as uuid } from "uuid";

class Todo {
    readonly id = uuid();
    public text = "";
    public checked = false;
    toggle() {
        this.checked = !this.checked;
    }
    delete() {
        // Get parent of the Todo, which is Array<Todo>
        const parent = Retree.parent(this);
        if (!Array.isArray(parent)) return;
        const index = parent.findIndex((c) => this.id === c.id);
        parent.splice(index, 1);
    }
}

class TodoList {
    public todos: Todo[] = [];
    add() {
        this.todos.push(new Todo());
    }
}

const tree = Retree.use(new TodoList());

// Listen for changes to the todo list — recursive to changes to any child node in todos
const unsubscribe = Retree.on(tree.todos, "treeChanged", (todos) => {
    console.log("list updated", todos);
});
tree.todos.add();

// Listen for changes to the todo node — only includes changes to child leafs and node references
const unsubscribeTodo = Retree.on(tree.todos[0], "nodeChanged", (todo) => {
    console.log("todo updated", todo);
});
tree.todos[0].toggle();
tree.todos[0].delete();

// Cleanup listeners
unsubscribe();
unsubscribeTodo();
```

You can find the complete JS repository via the [local clone on my computer](../../../../Libraries/Retree) to learn more about how it works.

The goal is to port this to C#, which obviously is a very different language. The core package requirements are as follows:

1. Use as much vanilla C# / .NET APIs as possible. This is a Unity project, but it would be nice to be able to port it elsewhere in the future.
2. Expose an abstract class called `RetreeNode`.
3. Class has a private `event` for `OnNodeChanged`, which is triggered whenever a child value changes.
4. Class has a public `OnNodeChanged` function, which takes a `listener` function as a param, which gets added to to `OnNodeChanged`. If this is the first listener registered, it should begin listening to changes to all child leafs / node references values via `ListenToNodeChanges` (more on #8).
5. Class has a public `OffNodeChanged` function, which takes a `listener` function as a param and does necessary cleanup and removes it from `OnNodeChanged`.
6. Class has a private `event` for `OnTreeChanged`, which is triggered whenever a child value for it and any other value in its child nodes.
7. Class has a public `OnTreeChanged` function, which takes a `listener` function as a param it sets to `OnTreeChanged`. The class should then begin listening to changes to it and all child nodes. Also has a `OffTreeChanged` (similar to #5, but for removing the tree listener).
8. A protected `ListenToNodeChanges` function should use reflection to listen to all child leafs. The changed values (and their keys) should be passed as a param to listeners (which will be used to track changes for when a node becomes detached from a tree). Whenever a leaf changes, invoke `OnNodeChanged`.
9. A private `ListenToTreeChanges` function should activate the listener for node changes, and recursively add `OnNodeChanged` listeners to all child leafs that extend `RetreeNode`. If any child node triggers `OnNodeChanged`, invoke `OnTreeChanged`. If a child node is deleted as a reference at any level below it the tree (as signaled by the params of the listener), unregister the `OnNodeChanged` listener. If a new node was added (as denoted by the params of the event), register a new listener for that node.
10. A private `OnChildNodeChanged` function, which is used as the listener for child node `OnNodeChanged` events (per #9).
11. Only listen to changes to value types. Getters/setters, readonly values, etc. are not supported for auto tree listening, even if that type is `RetreeNode`.

Example code example:

```csharp
using Retree;

public class TodoList : RetreeNode
{
    private List<Todo> _todos = new(); // listened to on "OnNodeChanged", all child values listened to "OnTreeChanged"
    public List<Todo> Todos => _todos; // not listened to

    public void Add(Todo todo)
    {
        _todos.Add(todo);
    }
}

public class Todo : RetreeNode
{
    public string Id { get; init; } // not listened to
    public bool checked = false; // listened to

    public void Toggle() {
        this._checked = !this._checked; // setting would trigger "OnNodeChanged" and/or "OnTreeChanged"
    }

    public Todo(string id, bool checked = false) : base()
    {
        Id = id;
        this.checked = checked;
    }
}

public class SomeExample
{
    private TodoList Todos = new();

    public void Start()
    {
        Todos.OnTreeChanged(OnTodosChanged);
        Todo todo = new("some id");
        Todos.Add(todo);
        todo.RegisterOnTreeChanges(OnTodoChanged);
    }

    public void Dispose()
    {
        Todos.OffTreeChanged(OnTodosChanged);
        Todos.Todos[0].OffNodeChanged(OnTodoChanged);
    }

    protected void OnTodosChanged(TreeChanges changes)
    {
        Debug.Log($"Any value in tree changed! {changes}");
    }

    protected void OnTodoChanged(NodeChanges<Todo> changes)
    {
        Debug.Log($"Any value in node changed! Specific key that triggered changed: {changes.keys}");
        Debug.Log($"id: {changes.node.Id}, checked: {changes.node.checked}");
    }
}
```

## Full spec (edit here)

### 1. Architecture overview

**Change detection strategy:** Polling with compiled expression delegates. C# has no equivalent to JS Proxies, so we snapshot field values and compare them each tick. Only nodes with active listeners are polled (lazy activation, matching the TS library's pattern).

**Tick model:** Three options — (1) manual `Retree.Tick()` for full control, (2) `Retree.StartTicks(rate)` for built-in auto-ticking via `System.Threading.Timer` (no Unity needed), (3) `RetreeUpdater` MonoBehaviour for Unity main-thread ticking.

**Collections:** Custom `RetreeList<T>` and `RetreeDictionary<TKey, TValue>` that wrap their standard counterparts and fire change events on mutation (Add, Remove, Insert, Clear, etc.). Unlike field polling, collection changes are detected synchronously at mutation time.

**Parent tracking:** Automatic. Parents are set when nodes are discovered as field values of other nodes or added to a `RetreeList<T>` / `RetreeDictionary<TKey, TValue>`. A node can only have one parent at a time.

**Tree propagation:** All upward tree change propagation flows through a single shared method `RetreeBase.PropagateAsTreeChange()`, which walks the `_parent` chain firing `OnTreeChanged` on each ancestor that has tree listeners. This avoids double-emission issues that could arise from mixing subscription callbacks with direct propagation.

**Transactions:** Both `RunTransaction` (batch into single emission) and `RunSilent` (suppress all events) are supported.

**Assembly:** Own asmdef at `Assets/Retree/Runtime/` with no Unity dependencies (`noEngineReferences: true`). Unity adapter in separate asmdef. `InternalsVisibleTo("Retree.Tests")` for test access.

---

### 2. Class hierarchy

```
RetreeBase (abstract)                      — common parent tracking + listener API
├── RetreeNode (abstract)                  — field-based change detection via polling
│   └── User classes (Todo, TodoList, etc.)
├── RetreeList<T> (sealed)                 — list mutation detection, T : RetreeNode
└── RetreeDictionary<TKey, TValue> (sealed) — dict mutation detection, TValue : RetreeNode
```

`RetreeBase` exists so `Retree.Parent(node)` can return a `RetreeNode`, `RetreeList<T>`, or `RetreeDictionary<TKey, TValue>`.

---

### 3. Class designs

#### 3.1 `RetreeBase` (abstract)

Common base for all reactive tree members.

```csharp
namespace Retree;

public abstract class RetreeBase
{
    // --- Parent tracking ---
    internal RetreeBase _parent;

    // --- Node change listeners ---
    private event Action<NodeChangedArgs> _onNodeChanged;
    private int _nodeListenerCount;

    public void OnNodeChanged(Action<NodeChangedArgs> listener);
    public void OffNodeChanged(Action<NodeChangedArgs> listener);

    // --- Tree change listeners ---
    private event Action<TreeChangedArgs> _onTreeChanged;
    private int _treeListenerCount;

    public void OnTreeChanged(Action<TreeChangedArgs> listener);
    public void OffTreeChanged(Action<TreeChangedArgs> listener);

    // --- Internal ---
    internal void EmitNodeChanged(NodeChangedArgs args);
    internal void EmitTreeChanged(TreeChangedArgs args);
    internal bool HasNodeListeners { get; }
    internal bool HasTreeListeners { get; }
    internal bool HasAnyListeners { get; }  // HasNodeListeners || HasTreeListeners

    /// Removes all node and tree listeners, calling
    /// OnLastNodeListenerRemoved / OnLastTreeListenerRemoved as needed.
    internal void ClearAllListeners();

    /// Shared upward tree propagation. Fires OnTreeChanged on self
    /// (if has tree listeners), then recurses to _parent.
    /// All tree propagation from RetreeNode, RetreeList, RetreeDictionary
    /// funnels through this single method.
    internal void PropagateAsTreeChange(RetreeBase sourceNode, IReadOnlyList<FieldChange> changes);

    // --- Lifecycle hooks (overridden by subclasses) ---
    protected virtual void OnFirstNodeListenerAdded() { }
    protected virtual void OnLastNodeListenerRemoved() { }
    protected virtual void OnFirstTreeListenerAdded() { }
    protected virtual void OnLastTreeListenerRemoved() { }
}
```

#### 3.2 `RetreeNode` (abstract)

Field-observable node. Users extend this.

```csharp
namespace Retree;

public abstract class RetreeNode : RetreeBase
{
    private FieldMetadata[] _fields;
    private object[] _fieldSnapshot;
    private bool _isListeningToNode;
    internal bool _isListeningToTree;   // internal so SubscribeToChild can set on children

    // Tracks OnNodeChanged subscriptions on child nodes/collections
    private readonly Dictionary<RetreeBase, Action<NodeChangedArgs>> _childListeners;

    // Tracks OnTreeChanged subscriptions on child collections
    // (needed so item field changes propagate through collections)
    private readonly Dictionary<RetreeBase, Action<TreeChangedArgs>> _childTreeListeners;

    // --- Lifecycle hooks ---
    // OnFirstNodeListenerAdded: if not listening, starts node changes
    // OnLastNodeListenerRemoved: if not tree-listening, stops node changes
    // OnFirstTreeListenerAdded: starts tree changes (ensures node listening active)
    // OnLastTreeListenerRemoved: stops tree changes; stops node if no node listeners

    // Discovers fields via FieldCache, takes snapshot, registers with RetreeRegistry.
    private void ListenToNodeChanges();

    // Ensures node listening, then subscribes to all child nodes/collections.
    private void ListenToTreeChanges();

    // Removes from RetreeRegistry, clears snapshot.
    private void StopListeningToNodeChanges();

    // Unsubscribes from all children.
    private void StopListeningToTreeChanges();

    // Subscribes to a child's OnNodeChanged. For RetreeNode children,
    // recursively sets up tree listening. For collection children
    // (RetreeList/RetreeDictionary), also subscribes to OnTreeChanged
    // so that item field changes propagate through the collection.
    private void SubscribeToChild(RetreeBase child);

    // Unsubscribes from a child's OnNodeChanged (and OnTreeChanged for
    // collections). For RetreeNode children, recursively tears down.
    private void UnsubscribeFromChild(RetreeBase child);

    // Called when a child node/collection fires OnNodeChanged.
    // Updates subscriptions for reference changes, then calls
    // PropagateAsTreeChange to walk changes upward.
    private void OnChildNodeChanged(RetreeBase child, NodeChangedArgs args);

    // Called when a child collection fires OnTreeChanged.
    // No-op handler — exists solely to keep the collection's tree
    // subscription alive (triggering OnFirstTreeListenerAdded).
    // Actual propagation is handled by PropagateAsTreeChange via _parent chain.
    private void OnChildCollectionTreeChanged(TreeChangedArgs args);

    // Called during Tick(). Compares fields to snapshot.
    // Fires EmitNodeChanged for direct changes.
    // Updates own subscriptions + emits self tree change (no upward propagation
    // — the parent's OnChildNodeChanged subscription handles that).
    internal void PollForChanges();
}
```

**Dual subscription pattern for collections:** When a `RetreeNode` subscribes to a child collection (e.g., `RetreeList<T>`), it registers two subscriptions:
1. **`OnNodeChanged`** — catches add/remove/replace mutations on the collection itself.
2. **`OnTreeChanged`** — triggers the collection's `OnFirstTreeListenerAdded()`, which subscribes to each item's `OnNodeChanged`. The callback itself (`OnChildCollectionTreeChanged`) is a no-op because the actual upward propagation is handled by `PropagateAsTreeChange` walking the `_parent` chain.

**Propagation separation (avoiding double-emission):** `PollForChanges` only handles self-level concerns: (1) fire `EmitNodeChanged`, (2) update own tree subscriptions, (3) emit self `OnTreeChanged`. It does NOT propagate upward. The parent's `OnChildNodeChanged` callback (triggered by step 1) calls `PropagateAsTreeChange` which handles all upward propagation. This ensures each change flows through exactly one path.

**Field discovery rules (what gets observed):**
- Instance fields only (not static, not const)
- Non-readonly fields only
- Excludes properties (get/set), even auto-properties (compiler-generated `<X>k__BackingField` excluded)
- Includes: value types (int, bool, float, enum, structs), `string`, `RetreeNode` references, `RetreeList<T>` references, `RetreeDictionary<TKey, TValue>` references
- Attribute `[RetreeIgnore]` to opt-out specific fields
- Walks inheritance chain up to (but not including) `RetreeNode` / `RetreeBase`

**Compiled delegate caching (via `FieldCache` / `FieldMetadata`):**
- Static `Dictionary<Type, FieldMetadata[]>` in `FieldCache` caches field info per concrete type
- Each `FieldMetadata` contains: `Name`, `FieldInfo`, compiled getter `Func<object, object>`, `FieldKind` (Value / RetreeNode / RetreeCollection), and `IsReferenceComparison`
- `FieldKind.RetreeCollection` covers both `RetreeList<>` and `RetreeDictionary<,>` (checked via generic type definition)
- Compiled once on first use of each type via `Expression.Lambda`

**Snapshot storage:**
- Each `RetreeNode` instance stores `object[] _fieldSnapshot` (one slot per observed field)
- On `PollForChanges()`: read current values via compiled delegates, compare to snapshot using `object.Equals()` (value types) or `ReferenceEquals` (reference types including `RetreeNode`, `RetreeList<T>`, `RetreeDictionary<TKey, TValue>`, `string`)
- Update snapshot after comparison
- During `TakeSnapshot()`, parent is auto-set on any discovered child `RetreeBase` whose `_parent` is null

#### 3.3 `RetreeList<T>` (sealed, `T : RetreeNode`)

Observable collection. Fires events synchronously on mutation.

```csharp
namespace Retree;

public sealed class RetreeList<T> : RetreeBase, IList<T>, IReadOnlyList<T> where T : RetreeNode
{
    private readonly List<T> _inner = new();

    // IList<T> / IReadOnlyList<T> implementation delegates to _inner,
    // but wraps mutating operations:

    public void Add(T item);        // sets item._parent, fires OnNodeChanged
    public bool Remove(T item);     // clears item._parent, fires OnNodeChanged
    public void Insert(int index, T item);
    public void RemoveAt(int index);
    public void Clear();            // clears all parents, fires OnNodeChanged
    public T this[int index] { get; set; }  // setter fires OnNodeChanged

    // When tree listeners are active, auto-subscribes to each
    // item's OnNodeChanged and propagates as OnTreeChanged.
}
```

**List change args:** When a list mutates, it fires `OnNodeChanged` with a `FieldChange` where:
- `FieldName` = `"Items"` (synthetic name for list content changes)
- `OldValue` / `NewValue` = the removed/added `RetreeNode` (or null)
- For `Clear()`: one `FieldChange` per removed item

**Parent tracking for items:**
- `Add(item)` → sets `item._parent = this`
- `Remove(item)` → sets `item._parent = null`
- If `item._parent` is already set to a different parent, throws `InvalidOperationException` ("Node already belongs to another parent. Remove it first.")

#### 3.4 `RetreeDictionary<TKey, TValue>` (sealed, `TValue : RetreeNode`)

Observable dictionary. Fires events synchronously on mutation, same pattern as `RetreeList<T>`.

```csharp
namespace Retree;

public sealed class RetreeDictionary<TKey, TValue> : RetreeBase, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TValue : RetreeNode
{
    private readonly Dictionary<TKey, TValue> _inner = new();

    // IDictionary / IReadOnlyDictionary implementation delegates to _inner,
    // but wraps mutating operations:

    public void Add(TKey key, TValue value);          // sets value._parent, fires OnNodeChanged
    public bool Remove(TKey key);                      // clears value._parent, fires OnNodeChanged
    public void Clear();                               // clears all parents, fires OnNodeChanged
    public TValue this[TKey key] { get; set; }         // setter fires OnNodeChanged (handles replace)

    public bool TryGetValue(TKey key, out TValue value);
    public bool ContainsKey(TKey key);
    public ICollection<TKey> Keys { get; }
    public ICollection<TValue> Values { get; }
    public int Count { get; }

    // When tree listeners are active, auto-subscribes to each
    // value's OnNodeChanged and propagates as OnTreeChanged.
}
```

**Dictionary change args:** When the dictionary mutates, it fires `OnNodeChanged` with a `FieldChange` where:
- `FieldName` = `key.ToString()` (the dictionary key that was affected, converted to string)
- `OldValue` / `NewValue` = the removed/added `RetreeNode` (or null)
- For `Clear()`: one `FieldChange` per removed entry
- For indexer replace (`dict[key] = newValue` where key already exists): `OldValue` = previous value, `NewValue` = new value

**Parent tracking for values:**
- `Add(key, value)` → sets `value._parent = this`
- `Remove(key)` → sets removed value's `_parent = null`
- Indexer replace → clears old value's parent, sets new value's parent
- If `value._parent` is already set to a different parent, throws `InvalidOperationException` ("Node already belongs to another parent. Remove it first.")

**Tree propagation:** Identical to `RetreeList<T>` — when tree listeners are active, subscribes to each value's `OnNodeChanged` and propagates changes upward as `OnTreeChanged`. Adding/removing entries updates subscriptions accordingly.

#### 3.5 `Retree` (static utility)

Active node tracking is delegated to `RetreeRegistry` (internal static class with `HashSet<RetreeNode>`), keeping the `Retree` class focused on public API.

```csharp
namespace Retree;

public static class Retree
{
    // --- Parent access ---
    public static RetreeBase Parent(RetreeBase node) => node._parent;

    // --- Tick (polls all active nodes for changes) ---
    public static void Tick();

    // --- Auto-tick ---
    private static System.Threading.Timer _tickTimer;

    public static void StartTicks(float tickRateSeconds);
    public static void StopTicks();
    public static bool IsTicking { get; }

    // --- Transaction batching ---
    internal static bool InTransaction { get; }   // true when _transactionDepth > 0
    internal static bool IsSilent { get; }

    public static void RunTransaction(Action action);
    public static void RunSilent(Action action);

    // --- Queue helpers (called by RetreeNode/List/Dict during transactions) ---
    internal static void QueueNodeChange(RetreeBase node, IReadOnlyList<FieldChange> changes);
    internal static void QueueTreeChange(RetreeBase listenerNode, RetreeBase sourceNode, IReadOnlyList<FieldChange> changes);

    // --- Listener cleanup ---
    public static void ClearListeners(RetreeBase node, bool recursive = false);

    // --- Reset (for testing — no Unity attribute since core has no Unity dep) ---
    internal static void Reset();  // StopTicks + clear all state + RetreeRegistry.Reset + FieldCache.Reset
}
```

**`StartTicks(float tickRateSeconds)` implementation:**
1. If already ticking, stop the existing timer first
2. Convert to milliseconds, clamp to minimum of 1ms
3. Create a `System.Threading.Timer` that calls `Tick()` at the specified interval
4. Set `IsTicking = true`
5. Note: callbacks fire on a thread pool thread. This is fine for pure C# consumers. Unity users should prefer `RetreeUpdater` for main-thread safety.

**`StopTicks()` implementation:**
1. Dispose the timer if it exists
2. Set `IsTicking = false`

**`Tick()` implementation:**
1. Copy `RetreeRegistry.ActiveNodes` to temporary list (avoids modification during iteration)
2. For each node, call `PollForChanges()`
3. Transaction queuing / emission is handled inside `PollForChanges` and the collection mutation methods

**`RunTransaction(Action)` implementation:**
1. Increment `_transactionDepth`
2. If outermost (depth == 1): create new `TransactionQueue`
3. Execute action (changes queue via `QueueNodeChange`/`QueueTreeChange`)
4. Decrement `_transactionDepth`
5. If outermost (depth == 0): flush queue (emits in insertion order)
6. Nested transactions are no-ops — they share the outermost queue

**`RunSilent(Action)` implementation:**
1. Set `_isSilent = true` (saves previous state for nesting)
2. Execute action
3. Restore previous `_isSilent` state
4. Snapshots are updated (so future Ticks don't re-detect the changes) but no events fire
5. Collection mutations update the collection but `EmitListChange`/`EmitDictChange` return early

**`ClearListeners(node, recursive)` implementation:**
1. Calls `node.ClearAllListeners()`
2. If recursive and node is `RetreeNode`: walks fields via `FieldCache`, recursing on child `RetreeBase` values
3. If recursive and node is `IEnumerable<RetreeNode>` (covers RetreeList): recursing on each item

#### 3.5 Event args

```csharp
namespace Retree;

public readonly struct FieldChange
{
    public string FieldName { get; }
    public object OldValue { get; }
    public object NewValue { get; }
}

public class NodeChangedArgs
{
    public RetreeBase Node { get; }
    public IReadOnlyList<FieldChange> Changes { get; }
}

public class TreeChangedArgs
{
    public RetreeBase ListenerNode { get; }  // node the treeChanged listener is on
    public RetreeBase SourceNode { get; }    // node whose fields actually changed
    public IReadOnlyList<FieldChange> Changes { get; }
}
```

#### 3.6 `[RetreeIgnore]` attribute

```csharp
namespace Retree;

[AttributeUsage(AttributeTargets.Field)]
public sealed class RetreeIgnoreAttribute : Attribute { }
```

Fields marked with this attribute are excluded from observation.

---

### 4. Change detection flow (detailed)

#### 4.1 Node change detection (polling)

```
OnNodeChanged() called on a RetreeNode
  → _nodeListenerCount increments from 0 to 1
  → ListenToNodeChanges() called
    → Discover fields via reflection (cached per type)
    → Compile getter delegates (cached per type)
    → Take initial snapshot of all field values
    → Add node to Retree._activeNodes

Retree.Tick() called
  → For each node in _activeNodes:
    → PollForChanges()
      → For each observed field:
        → Read current value via compiled delegate
        → Compare to snapshot (Equals for value types, ReferenceEquals for refs)
        → If different: record FieldChange { FieldName, OldValue, NewValue }
        → Update snapshot
      → If any changes detected and !Retree.IsSilent:
        → If Retree.InTransaction: queue NodeChangedArgs
        → Else: EmitNodeChanged(new NodeChangedArgs(this, changes))
```

#### 4.2 Tree change detection (recursive)

```
OnTreeChanged() called on a RetreeNode
  → _treeListenerCount increments from 0 to 1
  → OnFirstTreeListenerAdded() → ListenToTreeChanges()
    → Ensure ListenToNodeChanges() is active
    → SubscribeToChildren():
      → For each non-Value field in snapshot:
        → SubscribeToChild(child):
          → Register OnNodeChanged on child → OnChildNodeChanged
          → If child is RetreeNode:
            → Recursively ensure child._isListeningToTree
            → child.ListenToNodeChanges() + child.SubscribeToChildren()
          → If child is RetreeList/RetreeDictionary:
            → Also register OnTreeChanged on child → OnChildCollectionTreeChanged (no-op)
            → This triggers child's OnFirstTreeListenerAdded
            → Collection subscribes to each item's OnNodeChanged

OnChildNodeChanged(child, args) fires on a parent node
  → For each change in args.Changes:
    → If OldValue was RetreeBase: UnsubscribeFromChild (recursively)
    → If NewValue is RetreeBase: SubscribeToChild (recursively)
  → PropagateAsTreeChange(args.Node, args.Changes)
    → Fires OnTreeChanged on self (if tree listeners)
    → Walks _parent chain upward

PollForChanges() on the node itself (when _isListeningToTree)
  → After EmitNodeChanged:
    → Updates own child subscriptions (unsub old, sub new)
    → Emits self OnTreeChanged (no upward propagation — parent handles that)
```

#### 4.3 RetreeList change detection (synchronous)

```
RetreeList.Add(item) called
  → Validate item._parent is null (throw if not)
  → item._parent = this
  → _inner.Add(item)
  → If has node/tree listeners:
    → Fire OnNodeChanged with FieldChange { "Items", null, item }
  → If has tree listeners:
    → Subscribe to item.OnNodeChanged for tree propagation
```

---

### 5. Parent tracking rules

1. Every `RetreeBase` has an internal `_parent` field
2. Parent is set automatically:
   - When a `RetreeNode` field on a parent node is discovered to reference a child node (during first `TakeSnapshot()`)
   - When an item is added to a `RetreeList<T>` or value added to `RetreeDictionary<TKey, TValue>`
   - When a `RetreeNode` field changes from null to a node reference (detected during `PollForChanges`)
3. Parent is cleared automatically:
   - When a `RetreeNode` field changes from a node reference to null or a different node
   - When an item is removed from a `RetreeList<T>` or value removed from `RetreeDictionary<TKey, TValue>`
   - Only cleared if `ReferenceEquals(child._parent, this)` (safety check)
4. **Single parent rule:** A node can only have one parent. Adding a node that already has a parent throws `InvalidOperationException`. Users must remove from the old parent first.
5. `Retree.Parent(node)` returns the parent `RetreeBase` (which may be a `RetreeNode`, `RetreeList<T>`, or `RetreeDictionary<TKey, TValue>`)

---

### 6. Transaction & silent mode

**RunTransaction:**
- Multiple field changes to the same node during a transaction coalesce into a single `NodeChangedArgs` with all `FieldChange` entries
- Multiple `RetreeList` mutations coalesce similarly
- Tree changed events fire once per listener node, with the last source node's changes
- Transactions can nest (inner transactions are no-ops; emissions happen at outermost commit)

**RunSilent:**
- No events fire during the action
- Field snapshots are updated so changes aren't re-detected on next Tick()
- RetreeList mutations update the list but don't fire events
- Useful for initial setup or bulk imports where you don't want event storms

---

### 7. File structure

```
Assets/
  Retree/
    Runtime/
      Retree.Runtime.asmdef          (noEngineReferences: true)
      AssemblyInfo.cs                (InternalsVisibleTo "Retree.Tests")
      RetreeBase.cs
      RetreeNode.cs
      RetreeList.cs
      RetreeDictionary.cs
      Retree.cs                      (static utility class)
      NodeChangedArgs.cs
      TreeChangedArgs.cs
      FieldChange.cs
      RetreeIgnoreAttribute.cs
      Internal/
        FieldMetadata.cs             (FieldMetadata class + FieldKind enum)
        FieldCache.cs                (static per-type cache + compiled delegates)
        RetreeRegistry.cs            (static HashSet<RetreeNode> of active nodes)
        TransactionQueue.cs          (pending transaction state + TreeChangeEntry)
    Tests/
      Retree.Tests.asmdef            (references Runtime asmdef only, plain NUnit — no UnityEngine/UnityTest)
      RetreeNodeTests.cs             (10 tests)
      RetreeListTests.cs             (12 tests)
      RetreeDictionaryTests.cs       (11 tests)
      RetreeTreeChangedTests.cs      (11 tests)
      RetreeTransactionTests.cs      (7 tests)
      RetreeParentTests.cs           (10 tests)
      TestHelpers/
        TestNode.cs                  (SimpleNode, NodeWithChild, NodeWithList, NodeWithDict,
                                      NodeWithIgnored, NodeWithReadonly, NodeWithProperty,
                                      DeepChild, MiddleNode, RootNode)
    Unity/
      Retree.Unity.asmdef            (references Runtime asmdef + UnityEngine)
      RetreeUpdater.cs               (MonoBehaviour integration)
  Tests/
    Editor/
      Retree/
        Retree.UnityTests.asmdef     (references Retree.Unity + Retree.Runtime + UnityEngine.TestRunner)
        RetreeUpdaterTests.cs        (NOT YET IMPLEMENTED)

DotnetTests/                         (standalone test runner, no Unity dependency)
  Runtime/
    Retree.Runtime.csproj            (netstandard2.1, includes ../../Assets/Retree/Runtime/**/*.cs)
  Tests/
    Retree.Tests.csproj              (net9.0, NUnit 3.14, references Runtime csproj,
                                      includes ../../Assets/Retree/Tests/**/*.cs)
```

**Test strategy:**
- **Core tests** (`Assets/Retree/Tests/`): Plain NUnit, no Unity dependency. Test all core library functionality (field detection, list/dict mutations, tree propagation, parent tracking, transactions, silent mode, edge cases). Can run via `dotnet test DotnetTests/Tests/` outside Unity.
- **Unity tests** (`Assets/Tests/Editor/Retree/`): Unity Test Runner (EditMode). Test only Unity-specific integration — `RetreeUpdater` calling `Retree.Tick()` via MonoBehaviour lifecycle. Not yet implemented.

---

### 8. Unity adapter: `RetreeUpdater`

```csharp
// A simple MonoBehaviour
// Calls Retree.Tick() each frame
// Auto-creates via [RuntimeInitializeOnLoadMethod] or manual placement
public class RetreeUpdater : MonoBehaviour
{
    virtual protected void Start();
    virtual protected void OnDestroy();
    virtual protected void Update() => Retree.Tick();
    // ... other virtual lifecycle methods so inheriter can override
}
```

Users who don't use the Unity adapter can either:
- Call `Retree.Tick()` manually (e.g., in a test or custom game loop)
- Call `Retree.StartTicks(0.016f)` for built-in auto-ticking without Unity (fires on a thread pool thread)

---

### 9. Usage example (updated from base requirements)

```csharp
using Retree;

public class Todo : RetreeNode
{
    [RetreeIgnore]
    public string id;              // not observed (attributed)
    public string text = "";       // observed (field, value-like type)
    public bool isChecked = false; // observed (field, value type)

    public string Label => text;   // not observed (property)

    public void Toggle()
    {
        isChecked = !isChecked;    // detected on next Tick()
    }

    public void Delete()
    {
        var parent = Retree.Parent(this);
        if (parent is RetreeList<Todo> list)
        {
            list.Remove(this);     // fires immediately (list mutation)
        }
    }

    public Todo(string id) : base()
    {
        this.id = id;
    }
}

public class TodoList : RetreeNode
{
    private RetreeList<Todo> _todos = new();  // observed (RetreeList ref)
    public RetreeList<Todo> Todos => _todos;  // not observed (property)

    public void Add(string id)
    {
        _todos.Add(new Todo(id));
    }
}

// --- Consumer code ---

var todoList = new TodoList();

// Tree listener: fires when ANY descendant changes
todoList.OnTreeChanged(args =>
{
    Console.WriteLine($"Tree changed! Source: {args.SourceNode}, Changes: {args.Changes.Count}");
});

todoList.Add("todo-1");
Retree.Tick();  // fires treeChanged (new item in list)

todoList.Todos[0].Toggle();
Retree.Tick();  // fires treeChanged (isChecked changed on child)

// Node listener on specific todo
todoList.Todos[0].OnNodeChanged(args =>
{
    foreach (var change in args.Changes)
    {
        Console.WriteLine($"Field '{change.FieldName}': {change.OldValue} → {change.NewValue}");
    }
});

todoList.Todos[0].text = "Buy milk";
Retree.Tick();  // fires both nodeChanged on todo AND treeChanged on todoList

// Batch changes
Retree.RunTransaction(() =>
{
    todoList.Todos[0].text = "Buy eggs";
    todoList.Todos[0].isChecked = true;
    Retree.Tick();  // changes detected but emissions queued
});
// Single nodeChanged + single treeChanged fire here

// Cleanup
Retree.ClearListeners(todoList, recursive: true);
```

---

### 10. Benchmarks

The `Retree.Benchmarks` project is a standalone console app (`DotnetTests/Benchmarks/`) that measures change-detection performance across varying tree sizes and operation counts. It uses a custom `Stopwatch`-based harness rather than BenchmarkDotNet, since the metrics (per-change propagation depth, tick-count latency, operation-type breakdowns) don't fit a standard micro-benchmark model.

#### Tree models

| Size | Root type | Approx. nodes | Depth | Structure |
|------|-----------|--------------|-------|-----------|
| **Small** (~8) | `SmallStudioNode` | 8 | 3 | Studio → lead + 1 project (2 tasks) + 2 config entries |
| **Medium** (~100) | `MediumStudioNode` | 99 | 4 | Studio → CEO + 5 departments (5 members + 2 projects each, 3 tasks/project) + 5 config |
| **Large** (~500) | `ConglomerateNode` | 519 | 5 | Conglomerate → CEO + 5 divisions → 4 depts → manager + 5 members + 2 projects (5 tasks + 2 metadata each) + 10 global config |
| **XLarge** (~5K) | `ConglomerateNode` | 4,834 | 5 | Conglomerate → CEO + 10 divisions → 8 depts → manager + 12 members + 4 projects (8 tasks + 3 metadata each) + 20 global config |

All trees use a mix of:
- Primitive leaf fields (`string`, `int`, `bool`, `long`)
- Sub-`RetreeNode` references (e.g., `EmployeeNode`, `TaskNode`, `ConfigNode`)
- `RetreeList<T>` collections
- `RetreeDictionary<TKey, TValue>` collections

Shared node types reused across sizes: `EmployeeNode`, `TaskNode`, `ConfigNode` (defined in `Models/SharedNodes.cs`).

#### Operation counts

| Size   | Low | Med  | High | XHigh  |
|--------|-----|------|------|--------|
| Small  |   5 |  ~15 |  ~50 |   —    |
| Medium |  10 |  ~50 | ~200 |   —    |
| Large  |  20 | ~100 | ~500 |   —    |
| XLarge |  —  |   —  |   —  | 10,000 |

Operations are deterministic (not random) and include: root primitive changes, child node field changes, list item field changes, dict value field changes, list add/remove, dict add/remove, deep nested field changes, and reference swaps.

#### Metrics

| Metric | How measured |
|--------|-------------|
| **Tick duration** (ms) | `Stopwatch` around `Retree.Tick()` only |
| **Total scenario time** (ms) | `Stopwatch` around mutations + tick + emission |
| **Changes per tick** | Count of `TreeChangedArgs` received per `Tick()` call |
| **Propagation depth** | Walk `_parent` chain from source node to listener root |
| **Changes by operation type** | Categorize each `TreeChangedArgs` as `FieldChange`, `ListAdd`, `ListRemove`, `DictMutation`, or `ReferenceChange` |

Statistics computed for all timing metrics: **min, mean, median, P95, P99, max**.

#### Measurement protocol (per iteration)

1. `Retree.Reset()` — clean global state
2. Build tree via factory function
3. `OnTreeChanged` on root with `LatencyTracker` — records all changes
4. Initial `Retree.Tick()` to set up snapshots, then clear tracker
5. Execute all mutation operations (ops array)
6. `tracker.OnTickStart()` — mark tick boundary
7. Time `Retree.Tick()` with `BenchmarkTimer` — this is the "tick duration"
8. Record `totalTimer` (mutations + tick)
9. Drain change records from tracker

Warmup iterations (default 5) are run first and discarded. Between measured iterations, `GC.Collect()` is called for stable results.

#### CLI interface

```
dotnet run --project DotnetTests/Benchmarks -- [options]

Options:
  --size <small|medium|large|xlarge|all>  Tree size filter (default: all)
  --ops <low|medium|high|xhigh|all>      Operation count filter (default: all)
  --iterations <N>                    Measured iterations (default: 100)
  --warmup <N>                        Warmup iterations (default: 5)
  --verbose                           Print detailed per-scenario breakdown
  --help, -h                          Show this help
```

#### Output format

Summary mode prints three tables:
1. **Tick Duration (ms)** — per-scenario timing stats for `Retree.Tick()` only
2. **Total Scenario Time (ms)** — end-to-end timing including mutations and emissions
3. **Change Detection Summary** — total changes, mean propagation depth, mean changes per tick

Verbose mode (`--verbose`) additionally prints per-scenario detail:
- Full timing stats for tick duration, total time, and propagation depth
- Changes grouped by operation type with count and average depth

#### File structure

```
DotnetTests/Benchmarks/
├── Retree.Benchmarks.csproj
├── Program.cs                          — CLI entry, arg parsing, scenario assembly
├── Infrastructure/
│   ├── ChangeRecord.cs                — Readonly struct: timestamp, tick#, source type, field, depth, op type
│   ├── BenchmarkTimer.cs              — Stopwatch wrapper (Start/Stop/ElapsedMs)
│   ├── BenchmarkResult.cs             — Aggregated samples + Stats (min/max/mean/median/P95/P99)
│   ├── LatencyTracker.cs              — Hooks root treeChanged, records ChangeRecords
│   ├── BenchmarkScenario.cs           — Scenario config (factories, iterations)
│   ├── BenchmarkRunner.cs             — Orchestrates warmup + measured iterations
│   └── ConsoleReporter.cs             — ASCII table output
└── Models/
    ├── SharedNodes.cs                 — EmployeeNode, TaskNode, ConfigNode
    ├── SmallTreeFactory.cs            — SmallStudioNode + tree builder + Low/Med/High ops
    ├── MediumTreeFactory.cs           — MediumStudioNode + DepartmentNode + tree builder + ops
    ├── LargeTreeFactory.cs            — ConglomerateNode + DivisionNode + tree builder + ops
    └── XLargeTreeFactory.cs           — Reuses Large node types, 10 div × 8 dept, XHigh ops (~10K)
```

#### Sample output (50 iterations, net9.0, Apple Silicon)

```
  TICK DURATION (ms)
  +-------------+---------+---------+----------+----------+----------+----------+
  | Scenario    | Nodes   | Ops     | Min      | Mean     | Median   | P95      |
  +-------------+---------+---------+----------+----------+----------+----------+
  | Small/Low   | 8       | 5       | 0.0019   | 0.0025   | 0.0021   | 0.0043   |
  | Med/Med     | 99      | 45      | 0.0039   | 0.0044   | 0.0043   | 0.0056   |
  | Large/High  | 519     | 650     | 0.0024   | 0.0027   | 0.0026   | 0.0030   |
  | XL/XHigh    | 4834    | 10000   | 0.0057   | 0.0063   | 0.0063   | 0.0072   |
  +-------------+---------+---------+----------+----------+----------+----------+

  CHANGE DETECTION SUMMARY
  +-------------+----------------+-------------+-------------+
  | Scenario    | Total Changes  | Mean Depth  | Mean/Tick   |
  +-------------+----------------+-------------+-------------+
  | Small/Low   | 200            | 0.75        | 4.0         |
  | Med/Med     | 1000           | 1.40        | 20.0        |
  | Large/High  | 5100           | 0.99        | 102.0       |
  | XL/XHigh    | 20700          | 1.02        | 414.0       |
  +-------------+----------------+-------------+-------------+
```

---

### Acceptance criteria

1. **Field observation:** `RetreeNode` subclasses automatically detect changes to instance fields (non-readonly, non-static, non-property) after `Retree.Tick()` is called.
2. **Field filtering:** Properties, readonly fields, static fields, const fields, and `[RetreeIgnore]` fields are excluded from observation.
3. **Node changed event:** `OnNodeChanged` fires with `NodeChangedArgs` containing all `FieldChange` entries (field name, old value, new value) when any observed field on that node changes.
4. **Tree changed event:** `OnTreeChanged` fires with `TreeChangedArgs` when any descendant node's fields change. Includes both the listener node and the source node that changed.
5. **Lazy activation:** No polling occurs for nodes without active listeners. Adding the first listener activates polling; removing the last deactivates it.
6. **RetreeList mutations:** `Add`, `Remove`, `Insert`, `RemoveAt`, `Clear`, and index setter fire `OnNodeChanged` synchronously (no Tick required).
7. **RetreeList tree propagation:** Changes to items in a `RetreeList<T>` propagate as `OnTreeChanged` to any ancestor with tree listeners.
7b. **RetreeDictionary mutations:** `Add`, `Remove`, `Clear`, and indexer setter fire `OnNodeChanged` synchronously (no Tick required). `FieldChange.FieldName` is set to `key.ToString()`.
7c. **RetreeDictionary tree propagation:** Changes to values in a `RetreeDictionary<TKey, TValue>` propagate as `OnTreeChanged` to any ancestor with tree listeners.
8. **Parent tracking:** `Retree.Parent(node)` returns the correct parent after auto-tracking. Single-parent rule enforced with exception on violation.
9. **Transactions:** `Retree.RunTransaction()` batches all change events within the action into single emissions per node per event type.
10. **Silent mode:** `Retree.RunSilent()` suppresses all events but updates snapshots so changes aren't re-detected.
11. **Compiled delegates:** Field access uses compiled `Expression.Lambda` delegates, not raw `FieldInfo.GetValue()`.
12. **Per-type caching:** Field metadata and compiled delegates are cached once per concrete type, not per instance.
13. **Node reference tracking:** Changes to fields of type `RetreeNode` or `RetreeList<T>` fire `OnNodeChanged` with old/new references, enabling add/remove detection.
14. **Child listener management:** When a tree-listened node detects a child node reference change (old → new), it unsubscribes from the old child and subscribes to the new child automatically.
15. **No Unity dependency in core:** `Retree.Runtime.asmdef` has zero Unity assembly references. All core classes use only `System.*` APIs.
16. **Unity adapter:** `RetreeUpdater` calls `Retree.Tick()` via MonoBehaviour integration. Optional; core works without it.
17. **Static reset:** `Retree.Reset()` (internal) clears all static state including `RetreeRegistry`, `FieldCache`, transaction state, and auto-tick timer. Note: `[RuntimeInitializeOnLoadMethod]` attribute is NOT on the core class since it has no Unity dependency — the Unity adapter or consumer is responsible for calling `Reset()` on domain reload if needed.

### Edge cases

1. **Node added to two parents simultaneously:** Throws `InvalidOperationException`. User must remove from first parent before adding to second.
2. **Listener registered during callback:** Allowed. New listener fires on next change, not current.
3. **Node removed during tree callback:** The removed node's tree subscription is cleaned up. No stale references.
4. **Field changed back to original value within transaction:** If field goes A→B→A with Tick() calls between each, the A→B change IS detected and queued at the first Tick, and B→A IS detected and queued at the second Tick. Both are emitted when the transaction flushes (coalesced into a single `NodeChangedArgs`). Note: if no Tick() is called between the changes, only the net difference (none) is detected.
5. **RetreeList.Clear() with tree listeners:** Unsubscribes from all items, fires one `OnNodeChanged` with all removed items as `FieldChange` entries.
6. **Nested transactions:** Inner transactions are no-ops. All emissions happen when the outermost transaction completes.
7. **Tick() with no active nodes:** No-op. Zero cost.
8. **Field of type struct containing RetreeNode reference:** The struct field is compared by `Equals`. Nested RetreeNode references inside structs are NOT recursively tracked (only direct fields).
9. **Null node reference field → non-null:** Detected as a change. New child node gets parent set and subscribed for tree listening.
10. **RetreeList index setter (replace item):** Old item's parent cleared, new item's parent set. Both nodeChanged and tree subscription updated.
11. **Calling Tick() inside RunSilent:** Snapshots update but no events fire.
12. **Unregister last listener:** Node removed from `_activeNodes`. Snapshots cleared to free memory. Re-registering later takes a fresh snapshot (no "catch-up" events).
13. **Circular parent references:** Not possible due to single-parent rule. A node cannot be an ancestor of itself.
14. **RetreeDictionary indexer replace:** `dict[key] = newValue` where key exists — old value's parent cleared, new value's parent set. Old value unsubscribed, new value subscribed for tree propagation.
15. **RetreeDictionary key types:** Keys can be any type that works as a `Dictionary<TKey, TValue>` key (must implement proper `GetHashCode`/`Equals`). `FieldChange.FieldName` uses `key.ToString()`.

### Tasks

1. ✅ **Create file structure:** Set up `Assets/Retree/Runtime/` folder with `Retree.Runtime.asmdef` (no Unity refs, `noEngineReferences: true`). Set up `Assets/Retree/Unity/` with `Retree.Unity.asmdef`. Added `AssemblyInfo.cs` with `InternalsVisibleTo`. Set up `DotnetTests/` standalone test project for running outside Unity.
2. ✅ **Implement `FieldMetadata` and `FieldCache`:** Reflection-based field discovery, `Expression.Lambda` compiled delegate generation, per-type caching, `FieldKind` enum (Value / RetreeNode / RetreeCollection), `IsReferenceComparison` property, auto-property backing field exclusion.
3. ✅ **Implement `RetreeBase`:** Parent tracking, listener registration/unregistration, event emission, `PropagateAsTreeChange` (shared upward propagation), `ClearAllListeners`, `HasAnyListeners`, lifecycle hooks.
4. ✅ **Implement `FieldChange`, `NodeChangedArgs`, `TreeChangedArgs`:** Event arg types with constructors and `ToString()`.
5. ✅ **Implement `RetreeIgnoreAttribute`.**
6. ✅ **Implement `RetreeNode`:** `ListenToNodeChanges`, `PollForChanges`, snapshot storage, `ListenToTreeChanges`, `OnChildNodeChanged`, `OnChildCollectionTreeChanged`, `SubscribeToChild`/`UnsubscribeFromChild` with dual subscription for collections, `_childListeners` + `_childTreeListeners` dictionaries. Propagation separation: PollForChanges handles self-level only, parent subscription handles upward.
7. ✅ **Implement `RetreeList<T>`:** `IList<T>` + `IReadOnlyList<T>` wrapper, parent tracking on add/remove, synchronous change emission via `EmitListChange`/`EmitListChanges`, tree propagation via `PropagateAsTreeChange`.
8. ✅ **Implement `RetreeDictionary<TKey, TValue>`:** `IDictionary<TKey, TValue>` + `IReadOnlyDictionary<TKey, TValue>` wrapper, parent tracking on add/remove, synchronous change emission via `EmitDictChange`, tree propagation via `PropagateAsTreeChange`.
9. ✅ **Implement `Retree` static class:** `Tick()`, `StartTicks()`/`StopTicks()`, `RunTransaction()`, `RunSilent()`, `Parent()`, `ClearListeners()`, `QueueNodeChange`/`QueueTreeChange`, `Reset()`.
10. ✅ **Implement `RetreeRegistry`:** Static `HashSet<RetreeNode>` with `Register`/`Unregister`/`Reset`.
11. ✅ **Implement `TransactionQueue`:** Insertion-order tracking, coalescing node + tree changes per node, `TreeChangeEntry` struct, flush emits last tree entry per listener node.
12. ✅ **Write core tests (plain NUnit):** 61 tests across 6 test files — RetreeNodeTests (10), RetreeListTests (12), RetreeDictionaryTests (11), RetreeTreeChangedTests (11), RetreeTransactionTests (7), RetreeParentTests (10). All passing via `dotnet test`.
13. ✅ **Implement `RetreeUpdater`:** Unity adapter MonoBehaviour with virtual `Start`, `Update`, `OnDestroy`.
14. ⬜ **Write Unity integration tests:** RetreeUpdater auto-ticking via MonoBehaviour lifecycle. `RetreeUpdaterTests.cs` not yet implemented.
15. ⬜ **Run Unity EditMode tests:** Deferred until user is ready to use Unity test runner.
16. ✅ **Implement benchmark suite (`Retree.Benchmarks`):** Standalone console app in `DotnetTests/Benchmarks/`. Custom Stopwatch-based harness measuring tick duration, total scenario time, change detection counts, propagation depth, and operation-type breakdowns. Four tree sizes (Small ~8, Medium ~99, Large ~519, XLarge ~4,834 nodes) with 10 total scenarios including XL/XHigh (10K ops). CLI with `--size`, `--ops`, `--iterations`, `--warmup`, `--verbose` flags. ASCII table output. All 61 existing tests still pass after `InternalsVisibleTo` addition.

---

### Opportunities

Issues, improvements, and next steps discovered during implementation.

#### 🐛 Bugs / correctness gaps

1. **Deep tree propagation through collections is incomplete.** When a `RetreeList<T>` or `RetreeDictionary<TKey, TValue>` subscribes to items (via `OnFirstTreeListenerAdded`), it only calls `item.OnNodeChanged(listener)` — which starts polling the item's own fields but does NOT set up recursive tree listening on the item's children. This means if you have `Root → RetreeList<MiddleNode> → MiddleNode → DeepChild`, and `DeepChild.someField` changes, that change will NOT propagate up through the list to Root. It works for `Root → MiddleNode (field) → DeepChild` because `SubscribeToChild` recursively sets `_isListeningToTree = true` on direct RetreeNode children. **The fix:** In `RetreeList.SubscribeToItem` (and `RetreeDictionary.SubscribeToValue`), after registering on the item's `OnNodeChanged`, also recursively set up tree listening on the item — similar to what `RetreeNode.SubscribeToChild` does for RetreeNode children (set `_isListeningToTree`, call `ListenToNodeChanges`, `SubscribeToChildren`). A test should be added: `Root → RetreeList<MiddleNode> → MiddleNode → DeepChild.depth changes → Root sees TreeChanged`.

2. **`ClearListeners` with `RetreeDictionary` not fully covered.** `ClearListeners(node, recursive: true)` handles `RetreeNode` fields and `IEnumerable<RetreeNode>` (which covers `RetreeList<T>` since it implements `IEnumerable<T>`), but `RetreeDictionary<TKey, TValue>` implements `IEnumerable<KeyValuePair<TKey, TValue>>`, not `IEnumerable<RetreeNode>`. The recursive case may skip dictionary values. **The fix:** Add a check for `IDictionary` or iterate `.Values` on the dictionary in `ClearListeners`.

#### ⚠️ Potential issues

3. **Thread safety for `StartTicks`.** `System.Threading.Timer` fires callbacks on a thread pool thread, but none of the internal data structures (`RetreeRegistry`, `FieldCache`, `_fieldSnapshot`, `_childListeners`, etc.) are thread-safe. If `StartTicks` is used, all tree mutations must also happen on the same thread, or races will corrupt state. **Mitigation options:** (a) Document as "single-threaded only — `StartTicks` is for scenarios where all access is from the timer thread", (b) add a lock around `Tick()`, or (c) use `SynchronizationContext.Post` to marshal back to the calling thread.

4. **`FieldChange.OldValue` / `NewValue` box value types.** Since they're typed as `object`, every `int`, `bool`, `float` etc. gets boxed on every detected change. For high-frequency changes this creates GC pressure. **Mitigation:** Could introduce a generic `FieldChange<T>` in the future, but this would complicate the `IReadOnlyList<FieldChange>` in args. Alternatively, an object pool for boxed values.

5. **Transaction tree coalescing loses information.** `TransactionQueue.Flush()` emits only the *last* `TreeChangeEntry` per listener node. If multiple different source nodes changed during a transaction, all but the last source's changes are lost from the `TreeChangedArgs`. **Options:** (a) Emit once per unique (listener, source) pair, (b) aggregate all changes into a single `TreeChangedArgs` with a combined change list, or (c) document current behavior as intentional (batch = "something changed, re-read the tree").

6. **Auto-property backing field detection relies on compiler naming convention.** The check `field.Name.StartsWith("<") && field.Name.Contains(">k__BackingField")` works for the C# compiler but is technically an implementation detail. Other compilers or future C# versions could change this. Low risk, but worth noting.

#### 🔧 Improvements

7. **`RetreeUpdater` should use `[DisallowMultipleComponent]`.** Per the project's code conventions, MonoBehaviours should prefer this attribute.

8. **`RetreeUpdater` could use `UpdateHandler.cs`.** Per code conventions, adapting to the game's `OnUpdate` lifecycle via `UpdateHandler` rather than Unity's `Update()` would be more consistent. Requires understanding the project's `UpdateHandler` pattern.

9. **Add `[RuntimeInitializeOnLoadMethod]` reset in Unity adapter.** Since core `Retree.Reset()` can't have the Unity attribute (no Unity dep), the `RetreeUpdater` or a companion class in `Retree.Unity` should call `Retree.Reset()` on subsystem registration to handle domain reload in the editor.

10. **`RetreeList` lacks batch operations.** No `AddRange()`, `Sort()`, `RemoveAll()`, or `Move()`. Each of these would need to fire appropriate change events. `AddRange` could be wrapped in an implicit transaction for efficiency.

11. **No `IDisposable` pattern.** If a `RetreeNode` subclass is "disposed", there's no automatic listener cleanup. Users must call `Retree.ClearListeners()` manually. Could add `IDisposable` to `RetreeBase` that calls `ClearListeners(this, recursive: true)`.

12. **No weak reference mechanism for listeners.** If a listener lambda captures an object, that object won't be GC'd as long as the subscription is active. No "weak event pattern" equivalent. For long-lived trees, this could cause subtle memory leaks.

13. **Potential for stale snapshot on re-registration.** If a node has its listeners cleared, its snapshot is disposed. When a new listener is registered, `ListenToNodeChanges` takes a fresh snapshot. Any changes that occurred between unregister and re-register are silently absorbed (no catch-up event). This is documented behavior but could surprise users.

#### 🚀 Feature ideas

14. **Selector / computed values.** A `Retree.Select(node, selector)` API that fires only when a specific derived value changes, rather than on any field change. Similar to Redux selectors or MobX computed values.

15. **Diffing / patch generation.** Generate a patch object from `FieldChange` lists that can be serialized and applied elsewhere (e.g., for networking / state sync).

16. **`RetreeSet<T>`** — An observable `HashSet<T>` wrapper, completing the collection family alongside List and Dictionary.

17. **Middleware / interceptors.** Allow users to hook into the change pipeline (e.g., for logging, validation, or change rejection) before events are emitted.
