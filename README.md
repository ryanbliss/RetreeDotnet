# Retree

A reactive state tree library for .NET. Retree automatically detects field changes on your objects and propagates change events up the tree hierarchy — giving you fine-grained observability without manual event wiring.

Retree is a C# port of the TypeScript library [Retree](https://github.com/ryanbliss/retree).

## Features

- **Automatic change detection** — Fields on your nodes are polled each tick and compared to snapshots using compiled expression delegates for performance.
- **Tree propagation** — Changes on any descendant node bubble up to ancestors that have registered tree listeners.
- **Observable collections** — `RetreeList<T>` and `RetreeDictionary<TKey, TValue>` fire change events synchronously on mutation.
- **Parent tracking** — Nodes automatically know their parent in the tree.
- **Transactions** — Batch multiple mutations into a single event emission, or suppress events entirely with silent mode.
- **Zero dependencies** — Targets `netstandard2.1` with no external packages. Works in Unity, server apps, or anywhere .NET runs.

## Project Structure

```
RetreeDotnet/
├── src/RetreeCore/                    # Core library (netstandard2.1)
├── src/RetreeUnity/                   # Unity package (com.ryanbliss.retreecore)
│   ├── Runtime/                       # RetreeUpdater + precompiled DLL
├── samples/SpaceInvaders/             # Reference Unity project (Space Invaders game)
├── tests/RetreeCore.Tests/            # NUnit test suite (net9.0)
└── benchmarks/RetreeCore.Benchmarks/  # Performance benchmarks (net9.0)
```

## Installation

### Unity (via Package Manager)

Requires **Unity 6000.0+**.

Open **Window → Package Manager → + → Add package from git URL** and enter:

```
https://github.com/ryanbliss/RetreeDotnet.git?path=src/RetreeUnity
```

This installs the `com.ryanbliss.retreecore` package, which includes the precompiled `RetreeCore.dll` and the `RetreeUpdater` MonoBehaviour for main-thread ticking. No additional dependencies are required.

To pin a specific version, append `#<tag-or-commit>`:

```
https://github.com/ryanbliss/RetreeDotnet.git?path=src/RetreeUnity#v0.1.0
```

Or add it directly to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ryanbliss.retreecore": "https://github.com/ryanbliss/RetreeDotnet.git?path=src/RetreeUnity"
  }
}
```

### .NET (non-Unity)

Reference the `RetreeCore` project directly or build the DLL:

```bash
dotnet build src/RetreeCore/RetreeCore.csproj -c Release
```

The output DLL is at `src/RetreeCore/bin/Release/netstandard2.1/RetreeCore.dll`.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for tests and benchmarks)
- Any runtime supporting .NET Standard 2.1 (for the core library)

## Building

Build the entire solution:

```bash
dotnet build RetreeCore.sln
```

Build only the core library:

```bash
dotnet build src/RetreeCore/RetreeCore.csproj
```

## Running Tests

```bash
dotnet test tests/RetreeCore.Tests/RetreeCore.Tests.csproj
```

## Running Benchmarks

```bash
dotnet run --project benchmarks/RetreeCore.Benchmarks/RetreeCore.Benchmarks.csproj -c Release
```

Available CLI options:

| Option | Values | Default |
|--------|--------|---------|
| `--size` | `small`, `medium`, `large`, `xlarge`, `all` | `all` |
| `--ops` | `low`, `medium`, `high`, `xhigh`, `all` | `all` |
| `--iterations` | Any positive integer | `100` |
| `--warmup` | Any positive integer | `5` |
| `--verbose` | Flag | Off |

Example:

```bash
dotnet run --project benchmarks/RetreeCore.Benchmarks/RetreeCore.Benchmarks.csproj -c Release -- --size medium --ops low --iterations 50
```

## Quick Start

### Define Your Nodes

Extend `RetreeNode` and declare fields. Retree observes **instance fields** — not properties, not `readonly`, not `static`. Use `[RetreeIgnore]` to exclude a field.

```csharp
using RetreeCore;

public class Todo : RetreeNode
{
    [RetreeIgnore]
    public string id;
    public string text = "";
    public bool isComplete = false;

    public Todo(string id)
    {
        this.id = id;
    }

    public void Toggle()
    {
        isComplete = !isComplete;
    }
}

public class TodoList : RetreeNode
{
    public RetreeList<Todo> todos = new();

    public void Add(string text)
    {
        var todo = new Todo(Guid.NewGuid().ToString()) { text = text };
        todos.Add(todo);
    }

    public void Remove(Todo todo)
    {
        todos.Remove(todo);
    }
}
```

### Listen for Changes

```csharp
var list = new TodoList();

// Tree listener — fires when any descendant changes
list.RegisterOnTreeChanged(args =>
{
    Console.WriteLine($"Tree changed! Source: {args.SourceNode.GetType().Name}");
    foreach (var change in args.Changes)
    {
        Console.WriteLine($"  {change.FieldName}: {change.OldValue} -> {change.NewValue}");
    }
});

// Add a todo (synchronous — fires immediately since RetreeList detects mutations at call time)
list.Add("Buy groceries");
// Output:
//   Tree changed! Source: RetreeList`1
//   Items: (null) -> Todo

// Mutate a field (polling — requires a tick to detect)
list.todos[0].Toggle();
Retree.Tick();
// Output:
//   Tree changed! Source: Todo
//   isComplete: False -> True
```

### Node-Level Listening

For changes only on a specific node's own fields (not its descendants):

```csharp
var todo = list.todos[0];
todo.RegisterOnNodeChanged(args =>
{
    Console.WriteLine($"Todo changed:");
    foreach (var change in args.Changes)
    {
        Console.WriteLine($"  {change.FieldName}: {change.OldValue} -> {change.NewValue}");
    }
});

todo.text = "Buy milk instead";
Retree.Tick();
// Output:
//   Todo changed:
//   text: Buy groceries -> Buy milk instead
```

### Removing a Todo

Use `Retree.Parent()` to navigate the tree:

```csharp
public class Todo : RetreeNode
{
    // ... fields ...

    public void Delete()
    {
        var parent = Retree.Parent(this);
        if (parent is RetreeList<Todo> list)
        {
            list.Remove(this);
        }
    }
}
```

### Transactions

Batch multiple changes into a single event:

```csharp
Retree.RunTransaction(() =>
{
    list.Add("Task A");
    list.Add("Task B");
    list.Add("Task C");
    // All three additions emit as one batched event
});
```

### Silent Mode

Apply changes without firing any events:

```csharp
Retree.RunSilent(() =>
{
    list.todos[0].text = "Updated silently";
    Retree.Tick();
    // No events fire, but the snapshot is updated
});
```

### Auto-Ticking

Instead of calling `Retree.Tick()` manually, start automatic polling:

```csharp
// Tick every 50ms
Retree.StartTicks(0.05f);

// Later, stop ticking
Retree.StopTicks();
```

### Cleanup

```csharp
// Unregister specific listeners
list.UnregisterOnTreeChanged(myHandler);
todo.UnregisterOnNodeChanged(myHandler);

// Or clear all listeners on a node (optionally recursive)
Retree.ClearListeners(list, recursive: true);
```

## API Reference

### `RetreeNode`

Abstract base class for observable nodes. Extend this to define your state objects.

**Observed fields:** Instance, non-readonly, non-static fields of value types, `string`, `RetreeNode`, `RetreeList<T>`, or `RetreeDictionary<TKey, TValue>`. Exclude fields with `[RetreeIgnore]`.

### `RetreeList<T>` where `T : RetreeNode`

Observable list. Implements `IList<T>` and `IReadOnlyList<T>`. Mutations fire `NodeChanged` events synchronously with `FieldName = "Items"`.

### `RetreeDictionary<TKey, TValue>` where `TValue : RetreeNode`

Observable dictionary. Implements `IDictionary<TKey, TValue>` and `IReadOnlyDictionary<TKey, TValue>`. Mutations fire `NodeChanged` events synchronously with `FieldName = key.ToString()`.

### `Retree` (Static API)

| Method | Description |
|--------|-------------|
| `Retree.Tick()` | Poll all active nodes for field changes. |
| `Retree.StartTicks(float rate)` | Begin auto-ticking at the given interval in seconds. |
| `Retree.StopTicks()` | Stop auto-ticking. |
| `Retree.IsTicking` | Whether auto-ticking is active. |
| `Retree.Parent(RetreeBase node)` | Get a node's parent in the tree. |
| `Retree.RunTransaction(Action action)` | Batch events during `action` into a single emission. |
| `Retree.RunSilent(Action action)` | Suppress all events during `action`. |
| `Retree.ClearListeners(RetreeBase node, bool recursive)` | Remove all listeners from a node. |

### Event Args

| Type | Properties |
|------|------------|
| `NodeChangedArgs` | `Node`, `Changes` (list of `FieldChange`) |
| `TreeChangedArgs` | `ListenerNode`, `SourceNode`, `Changes` (list of `FieldChange`) |
| `FieldChange` | `FieldName`, `OldValue`, `NewValue` |

## Licensing & Copyright

Copyright (c) Ryan Bliss. All rights reserved. Licensed under MIT license.

See [LICENSE](LICENSE) for details.
