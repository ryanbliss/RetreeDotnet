// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace RetreeCore.Benchmarks.Models
{
    public class SmallProjectNode : RetreeNode
    {
        public string title = "";
        public int priority = 0;
        public bool complete = false;
        private RetreeList<TaskNode> _tasks = new RetreeList<TaskNode>();
        public RetreeList<TaskNode> Tasks => _tasks;
    }

    public class SmallStudioNode : RetreeNode
    {
        public string name = "Studio";
        public int budget = 100000;
        public bool active = true;
        public EmployeeNode lead;
        private RetreeList<SmallProjectNode> _projects = new RetreeList<SmallProjectNode>();
        public RetreeList<SmallProjectNode> Projects => _projects;
        private RetreeDictionary<string, ConfigNode> _settings = new RetreeDictionary<string, ConfigNode>();
        public RetreeDictionary<string, ConfigNode> Settings => _settings;
    }

    public static class SmallTreeFactory
    {
        public static SmallStudioNode Build()
        {
            var root = new SmallStudioNode
            {
                name = "Indie Studio",
                budget = 50000,
                active = true,
                lead = new EmployeeNode { name = "Alex", salary = 80000, level = 3 }
            };

            var project = new SmallProjectNode { title = "First Game", priority = 1, complete = false };
            project.Tasks.Add(new TaskNode { description = "Design levels", hours = 40, done = false });
            project.Tasks.Add(new TaskNode { description = "Write music", hours = 20, done = false });
            root.Projects.Add(project);

            root.Settings.Add("theme", new ConfigNode { key = "theme", value = "dark", version = 1 });
            root.Settings.Add("lang", new ConfigNode { key = "lang", value = "en", version = 1 });

            return root;
        }

        public static Action[] LowOps(RetreeBase rootBase)
        {
            var root = (SmallStudioNode)rootBase;
            return new Action[]
            {
                () => root.budget = root.budget + 1000,
                () => root.name = "Updated Studio",
                () => root.lead.salary = root.lead.salary + 5000,
                () => root.Projects[0].Tasks[0].hours = 50,
                () => root.Settings["theme"].value = "light",
            };
        }

        public static Action[] MediumOps(RetreeBase rootBase)
        {
            var root = (SmallStudioNode)rootBase;
            var ops = new List<Action>();

            // Root primitives (3)
            ops.Add(() => root.budget = 60000);
            ops.Add(() => root.active = false);
            ops.Add(() => root.name = "Renamed Studio");

            // Child node fields (2)
            ops.Add(() => root.lead.salary = 90000);
            ops.Add(() => root.lead.level = 4);

            // List item fields (2)
            ops.Add(() => root.Projects[0].title = "Second Game");
            ops.Add(() => root.Projects[0].Tasks[0].done = true);

            // Dict value fields (2)
            ops.Add(() => root.Settings["theme"].version = 2);
            ops.Add(() => root.Settings["lang"].value = "fr");

            // List mutations (2)
            ops.Add(() => root.Projects[0].Tasks.Add(new TaskNode { description = "Testing", hours = 10 }));
            ops.Add(() => root.Projects.Add(new SmallProjectNode { title = "Side Project", priority = 3 }));

            // Dict mutations (2)
            ops.Add(() => root.Settings.Add("font", new ConfigNode { key = "font", value = "mono", version = 1 }));
            ops.Add(() => root.Settings.Remove("lang"));

            return ops.ToArray();
        }

        public static Action[] HighOps(RetreeBase rootBase)
        {
            var root = (SmallStudioNode)rootBase;
            var ops = new List<Action>();
            int counter = 0;

            // 10 root primitives
            for (int i = 0; i < 5; i++)
            {
                var val = i;
                ops.Add(() => root.budget = 50000 + val * 1000);
                ops.Add(() => root.name = "Studio_" + (++counter));
            }

            // 8 child node fields
            for (int i = 0; i < 4; i++)
            {
                var val = i;
                ops.Add(() => root.lead.salary = 80000 + val * 100);
                ops.Add(() => root.lead.level = val + 1);
            }

            // 10 list item fields
            for (int i = 0; i < 5; i++)
            {
                var val = i;
                ops.Add(() => root.Projects[0].priority = val);
                ops.Add(() => root.Projects[0].Tasks[0].hours = 10 + val);
            }

            // 6 dict value fields
            for (int i = 0; i < 3; i++)
            {
                var val = i;
                ops.Add(() => root.Settings["theme"].value = "theme_" + val);
                ops.Add(() => root.Settings["theme"].version = val + 1);
            }

            // 8 list mutations (add then remove cycles)
            for (int i = 0; i < 4; i++)
            {
                var idx = i;
                ops.Add(() => root.Projects[0].Tasks.Add(new TaskNode { description = "Task_" + idx, hours = idx }));
                ops.Add(() => root.Projects[0].Tasks.RemoveAt(root.Projects[0].Tasks.Count - 1));
            }

            // 8 dict mutations
            for (int i = 0; i < 4; i++)
            {
                var key = "temp_" + i;
                ops.Add(() => root.Settings.Add(key, new ConfigNode { key = key, value = "v", version = 1 }));
                ops.Add(() => root.Settings.Remove(key));
            }

            return ops.ToArray();
        }
    }
}
