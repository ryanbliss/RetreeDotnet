// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using Retree.Unity;
using UnityEngine;

namespace Retree.Samples.TodoApp
{
    /// <summary>
    /// Demonstrates Retree usage in Unity with a simple todo list.
    /// Attach this to a GameObject alongside a RetreeUpdater component.
    /// </summary>
    public class TodoAppExample : MonoBehaviour
    {
        private TodoList _todoList;

        private void Start()
        {
            // Ensure a RetreeUpdater exists to drive Retree.Tick() each frame.
            if (FindFirstObjectByType<RetreeUpdater>() == null)
            {
                gameObject.AddComponent<RetreeUpdater>();
            }

            _todoList = new TodoList();

            // Listen for any change anywhere in the tree.
            _todoList.RegisterOnTreeChanged(OnTreeChanged);

            // Add some todos. RetreeList mutations fire synchronously.
            var grocery = _todoList.Add("Buy groceries");
            _todoList.Add("Walk the dog");
            _todoList.Add("Write code");

            // Toggle a todo. This is a field change detected on the next tick.
            grocery.Toggle();

            Debug.Log($"[TodoApp] Created {_todoList.todos.Count} todos. " +
                      $"First todo isComplete will be detected next frame.");
        }

        private void OnTreeChanged(TreeChangedArgs args)
        {
            foreach (var change in args.Changes)
            {
                Debug.Log($"[TodoApp] Tree changed — " +
                          $"Source: {args.SourceNode.GetType().Name}, " +
                          $"Field: {change.FieldName}, " +
                          $"Old: {change.OldValue}, New: {change.NewValue}");
            }
        }

        private void OnDestroy()
        {
            if (_todoList != null)
            {
                _todoList.UnregisterOnTreeChanged(OnTreeChanged);
                Retree.ClearListeners(_todoList, recursive: true);
            }
        }
    }
}
