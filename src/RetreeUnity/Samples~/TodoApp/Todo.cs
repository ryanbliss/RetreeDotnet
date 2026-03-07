// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

namespace Retree.Samples.TodoApp
{
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

        public void Delete()
        {
            var parent = Retree.Parent(this);
            if (parent is RetreeList<Todo> list)
            {
                list.Remove(this);
            }
        }
    }
}
