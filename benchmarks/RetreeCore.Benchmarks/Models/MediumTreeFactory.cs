// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace RetreeCore.Benchmarks.Models
{
    public class MediumProjectNode : RetreeNode
    {
        public string title = "";
        public int priority = 0;
        public bool complete = false;
        public int deadline = 0;
        private RetreeList<TaskNode> _tasks = new RetreeList<TaskNode>();
        public RetreeList<TaskNode> Tasks => _tasks;
    }

    public class DepartmentNode : RetreeNode
    {
        public string name = "";
        public int budget = 0;
        public int headcount = 0;
        public EmployeeNode manager;
        private RetreeList<EmployeeNode> _members = new RetreeList<EmployeeNode>();
        public RetreeList<EmployeeNode> Members => _members;
        private RetreeList<MediumProjectNode> _projects = new RetreeList<MediumProjectNode>();
        public RetreeList<MediumProjectNode> Projects => _projects;
    }

    public class MediumStudioNode : RetreeNode
    {
        public string name = "MediumStudio";
        public int budget = 1000000;
        public bool active = true;
        public int foundedYear = 2020;
        public EmployeeNode ceo;
        private RetreeList<DepartmentNode> _departments = new RetreeList<DepartmentNode>();
        public RetreeList<DepartmentNode> Departments => _departments;
        private RetreeDictionary<string, ConfigNode> _config = new RetreeDictionary<string, ConfigNode>();
        public RetreeDictionary<string, ConfigNode> Config => _config;
    }

    public static class MediumTreeFactory
    {
        public static MediumStudioNode Build()
        {
            var root = new MediumStudioNode
            {
                name = "GameWorks",
                budget = 5000000,
                active = true,
                foundedYear = 2015,
                ceo = new EmployeeNode { name = "CEO Jane", salary = 200000, level = 10 }
            };

            var deptNames = new[] { "Engineering", "Art", "Design", "QA", "Marketing" };
            for (int d = 0; d < 5; d++)
            {
                var dept = new DepartmentNode
                {
                    name = deptNames[d],
                    budget = 500000 + d * 100000,
                    headcount = 5,
                    manager = new EmployeeNode { name = $"Manager_{d}", salary = 120000, level = 7 }
                };

                for (int m = 0; m < 5; m++)
                {
                    dept.Members.Add(new EmployeeNode
                    {
                        name = $"Emp_{d}_{m}",
                        salary = 60000 + m * 5000,
                        level = m + 1,
                        active = true
                    });
                }

                for (int p = 0; p < 2; p++)
                {
                    var project = new MediumProjectNode
                    {
                        title = $"Project_{d}_{p}",
                        priority = p + 1,
                        complete = false,
                        deadline = 20260601 + d * 100 + p
                    };
                    for (int t = 0; t < 3; t++)
                    {
                        project.Tasks.Add(new TaskNode
                        {
                            description = $"Task_{d}_{p}_{t}",
                            hours = 8 + t * 4,
                            done = false
                        });
                    }
                    dept.Projects.Add(project);
                }

                root.Departments.Add(dept);
            }

            for (int c = 0; c < 5; c++)
            {
                root.Config.Add($"config_{c}", new ConfigNode
                {
                    key = $"config_{c}",
                    value = $"value_{c}",
                    version = 1
                });
            }

            return root;
        }

        public static Action[] LowOps(RetreeBase rootBase)
        {
            var root = (MediumStudioNode)rootBase;
            return new Action[]
            {
                // Root level (3)
                () => root.budget = root.budget + 100000,
                () => root.name = "GameWorks Updated",
                () => root.active = false,
                // Department level (3)
                () => root.Departments[0].budget = 600000,
                () => root.Departments[1].name = "Creative Arts",
                () => root.Departments[0].manager.salary = 130000,
                // Member level (2)
                () => root.Departments[0].Members[0].salary = 70000,
                () => root.Departments[2].Members[3].level = 5,
                // Config (2)
                () => root.Config["config_0"].value = "updated_0",
                () => root.Config["config_2"].version = 2,
            };
        }

        public static Action[] MediumOps(RetreeBase rootBase)
        {
            var root = (MediumStudioNode)rootBase;
            var ops = new List<Action>();

            // Root (5)
            ops.Add(() => root.budget = 6000000);
            ops.Add(() => root.name = "GameWorks Pro");
            ops.Add(() => root.active = false);
            ops.Add(() => root.foundedYear = 2014);
            ops.Add(() => root.ceo.salary = 250000);

            // Department primitives (10)
            for (int d = 0; d < 5; d++)
            {
                var di = d;
                ops.Add(() => root.Departments[di].budget = 700000 + di * 10000);
                ops.Add(() => root.Departments[di].headcount = 6);
            }

            // Member fields (10)
            for (int d = 0; d < 5; d++)
            {
                var di = d;
                ops.Add(() => root.Departments[di].Members[0].salary = 75000);
                ops.Add(() => root.Departments[di].Members[1].active = false);
            }

            // Task fields (10)
            for (int d = 0; d < 5; d++)
            {
                var di = d;
                ops.Add(() => root.Departments[di].Projects[0].Tasks[0].done = true);
                ops.Add(() => root.Departments[di].Projects[0].Tasks[1].hours = 20);
            }

            // List mutations (5)
            ops.Add(() => root.Departments[0].Members.Add(new EmployeeNode { name = "NewHire", salary = 55000 }));
            ops.Add(() => root.Departments[1].Projects[0].Tasks.Add(new TaskNode { description = "Urgent", hours = 4 }));
            ops.Add(() => root.Departments[2].Members.RemoveAt(root.Departments[2].Members.Count - 1));
            ops.Add(() => root.Departments[3].Projects.Add(new MediumProjectNode { title = "Bonus", priority = 5 }));
            ops.Add(() => root.Departments[4].Members.Add(new EmployeeNode { name = "Intern", salary = 30000 }));

            // Dict mutations (5)
            ops.Add(() => root.Config.Add("new_key", new ConfigNode { key = "new_key", value = "new_val", version = 1 }));
            ops.Add(() => root.Config["config_1"].version = 3);
            ops.Add(() => root.Config["config_3"].value = "changed");
            ops.Add(() => root.Config.Remove("config_4"));
            ops.Add(() => root.Config.Add("extra", new ConfigNode { key = "extra", value = "data", version = 1 }));

            return ops.ToArray();
        }

        public static Action[] HighOps(RetreeBase rootBase)
        {
            var root = (MediumStudioNode)rootBase;
            var ops = new List<Action>();
            int c = 0;

            // Root/CEO (20)
            for (int i = 0; i < 10; i++)
            {
                var v = i;
                ops.Add(() => root.budget = 5000000 + v * 10000);
                ops.Add(() => root.ceo.salary = 200000 + v * 1000);
            }

            // Dept primitives (20)
            for (int d = 0; d < 5; d++)
            {
                var di = d;
                for (int i = 0; i < 4; i++)
                {
                    var vi = i;
                    ops.Add(() => root.Departments[di].budget = 500000 + vi * 10000);
                }
            }

            // Member field changes (50)
            for (int d = 0; d < 5; d++)
            {
                var di = d;
                for (int m = 0; m < 5; m++)
                {
                    var mi = m;
                    ops.Add(() => root.Departments[di].Members[mi].salary = 60000 + (++c) * 100);
                    ops.Add(() => root.Departments[di].Members[mi].level = (c % 10) + 1);
                }
            }

            // Task field changes (30)
            for (int d = 0; d < 5; d++)
            {
                var di = d;
                for (int p = 0; p < 2; p++)
                {
                    var pi = p;
                    for (int t = 0; t < 3; t++)
                    {
                        var ti = t;
                        ops.Add(() => root.Departments[di].Projects[pi].Tasks[ti].hours = 10 + (++c));
                    }
                }
            }

            // List add/remove cycles (30)
            for (int d = 0; d < 5; d++)
            {
                var di = d;
                for (int i = 0; i < 3; i++)
                {
                    var idx = i;
                    ops.Add(() => root.Departments[di].Members.Add(new EmployeeNode { name = $"Temp_{di}_{idx}", salary = 40000 }));
                    ops.Add(() => root.Departments[di].Members.RemoveAt(root.Departments[di].Members.Count - 1));
                }
            }

            // Dict mutations (20)
            for (int i = 0; i < 10; i++)
            {
                var key = $"dyn_{i}";
                ops.Add(() => root.Config.Add(key, new ConfigNode { key = key, value = "v", version = 1 }));
                ops.Add(() => root.Config.Remove(key));
            }

            // Reference swaps (10)
            for (int d = 0; d < 5; d++)
            {
                var di = d;
                ops.Add(() => root.Departments[di].manager = new EmployeeNode { name = $"NewMgr_{di}", salary = 130000, level = 8 });
                ops.Add(() => root.Departments[di].headcount = root.Departments[di].headcount + 1);
            }

            return ops.ToArray();
        }
    }
}
