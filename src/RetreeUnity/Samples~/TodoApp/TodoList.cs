// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;

namespace RetreeCore.Samples.TodoApp
{
    public class TodoList : RetreeNode
    {
        public RetreeList<Todo> todos = new();

        public Todo Add(string text)
        {
            var todo = new Todo(Guid.NewGuid().ToString()) { text = text };
            todos.Add(todo);
            return todo;
        }

        public void Remove(Todo todo)
        {
            todos.Remove(todo);
        }
    }
}
