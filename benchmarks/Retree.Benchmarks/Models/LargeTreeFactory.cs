// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Retree.Benchmarks.Models
{
    public class LargeProjectNode : RetreeNode
    {
        public string title = "";
        public int priority = 0;
        public int storyPoints = 0;
        public bool complete = false;
        private RetreeList<TaskNode> _tasks = new RetreeList<TaskNode>();
        public RetreeList<TaskNode> Tasks => _tasks;
        private RetreeDictionary<string, ConfigNode> _metadata = new RetreeDictionary<string, ConfigNode>();
        public RetreeDictionary<string, ConfigNode> Metadata => _metadata;
    }

    public class LargeDepartmentNode : RetreeNode
    {
        public string name = "";
        public int budget = 0;
        public int headcount = 0;
        public EmployeeNode manager;
        private RetreeList<EmployeeNode> _members = new RetreeList<EmployeeNode>();
        public RetreeList<EmployeeNode> Members => _members;
        private RetreeList<LargeProjectNode> _projects = new RetreeList<LargeProjectNode>();
        public RetreeList<LargeProjectNode> Projects => _projects;
    }

    public class DivisionNode : RetreeNode
    {
        public string name = "";
        public int budget = 0;
        public string region = "";
        public EmployeeNode vp;
        private RetreeList<LargeDepartmentNode> _departments = new RetreeList<LargeDepartmentNode>();
        public RetreeList<LargeDepartmentNode> Departments => _departments;
    }

    public class ConglomerateNode : RetreeNode
    {
        public string name = "MegaCorp";
        public long revenue = 10000000;
        public int employeeCount = 5000;
        public bool publiclyTraded = true;
        public EmployeeNode ceo;
        private RetreeList<DivisionNode> _divisions = new RetreeList<DivisionNode>();
        public RetreeList<DivisionNode> Divisions => _divisions;
        private RetreeDictionary<string, ConfigNode> _globalConfig = new RetreeDictionary<string, ConfigNode>();
        public RetreeDictionary<string, ConfigNode> GlobalConfig => _globalConfig;
    }

    public static class LargeTreeFactory
    {
        public static ConglomerateNode Build()
        {
            var root = new ConglomerateNode
            {
                name = "MegaCorp International",
                revenue = 50000000,
                employeeCount = 500,
                publiclyTraded = true,
                ceo = new EmployeeNode { name = "CEO Supreme", salary = 500000, level = 15 }
            };

            var regions = new[] { "North America", "Europe", "Asia Pacific", "Latin America", "Africa" };
            for (int div = 0; div < 5; div++)
            {
                var division = new DivisionNode
                {
                    name = $"Division_{div}",
                    budget = 5000000 + div * 1000000,
                    region = regions[div],
                    vp = new EmployeeNode { name = $"VP_{div}", salary = 300000, level = 12 }
                };

                for (int d = 0; d < 4; d++)
                {
                    var dept = new LargeDepartmentNode
                    {
                        name = $"Dept_{div}_{d}",
                        budget = 500000 + d * 50000,
                        headcount = 5,
                        manager = new EmployeeNode { name = $"Mgr_{div}_{d}", salary = 150000, level = 8 }
                    };

                    for (int m = 0; m < 5; m++)
                    {
                        dept.Members.Add(new EmployeeNode
                        {
                            name = $"Emp_{div}_{d}_{m}",
                            salary = 60000 + m * 5000,
                            level = m + 1,
                            active = true
                        });
                    }

                    for (int p = 0; p < 2; p++)
                    {
                        var project = new LargeProjectNode
                        {
                            title = $"Proj_{div}_{d}_{p}",
                            priority = p + 1,
                            storyPoints = 20 + p * 10,
                            complete = false
                        };

                        for (int t = 0; t < 5; t++)
                        {
                            project.Tasks.Add(new TaskNode
                            {
                                description = $"Task_{div}_{d}_{p}_{t}",
                                hours = 4 + t * 2,
                                done = false
                            });
                        }

                        for (int md = 0; md < 2; md++)
                        {
                            var key = $"meta_{div}_{d}_{p}_{md}";
                            project.Metadata.Add(key, new ConfigNode
                            {
                                key = key,
                                value = $"val_{md}",
                                version = 1
                            });
                        }

                        dept.Projects.Add(project);
                    }

                    division.Departments.Add(dept);
                }

                root.Divisions.Add(division);
            }

            for (int gc = 0; gc < 10; gc++)
            {
                root.GlobalConfig.Add($"global_{gc}", new ConfigNode
                {
                    key = $"global_{gc}",
                    value = $"gval_{gc}",
                    version = 1
                });
            }

            return root;
        }

        public static Action[] LowOps(RetreeBase rootBase)
        {
            var root = (ConglomerateNode)rootBase;
            return new Action[]
            {
                // Root level (5)
                () => root.revenue = root.revenue + 1000000,
                () => root.employeeCount = root.employeeCount + 10,
                () => root.name = "MegaCorp Updated",
                () => root.ceo.salary = 550000,
                () => root.publiclyTraded = false,
                // Division level (5)
                () => root.Divisions[0].budget = 6000000,
                () => root.Divisions[1].name = "Europe Div Renamed",
                () => root.Divisions[2].vp.salary = 320000,
                () => root.Divisions[3].region = "LATAM",
                () => root.Divisions[4].budget = 4000000,
                // Deep project/task level (5)
                () => root.Divisions[0].Departments[0].Projects[0].Tasks[0].done = true,
                () => root.Divisions[1].Departments[1].Projects[0].storyPoints = 50,
                () => root.Divisions[2].Departments[0].Projects[1].Tasks[2].hours = 16,
                () => root.Divisions[0].Departments[2].Projects[0].title = "Renamed Project",
                () => root.Divisions[3].Departments[0].Projects[0].Tasks[4].description = "Updated task",
                // Config (5)
                () => root.GlobalConfig["global_0"].value = "changed_0",
                () => root.GlobalConfig["global_5"].version = 2,
                () => root.Divisions[0].Departments[0].Projects[0].Metadata.Values.GetEnumerator().MoveNext(),
                () => root.GlobalConfig["global_9"].value = "changed_9",
                () => root.GlobalConfig["global_3"].version = 3,
            };
        }

        public static Action[] MediumOps(RetreeBase rootBase)
        {
            var root = (ConglomerateNode)rootBase;
            var ops = new List<Action>();

            // Root (10)
            for (int i = 0; i < 5; i++)
            {
                var v = i;
                ops.Add(() => root.revenue = 50000000 + v * 100000);
                ops.Add(() => root.employeeCount = 500 + v);
            }

            // Division/dept fields (20)
            for (int div = 0; div < 5; div++)
            {
                var di = div;
                ops.Add(() => root.Divisions[di].budget = 5000000 + di * 500000);
                ops.Add(() => root.Divisions[di].name = $"Div_{di}_updated");
                ops.Add(() => root.Divisions[di].Departments[0].budget = 600000);
                ops.Add(() => root.Divisions[di].Departments[1].headcount = 6);
            }

            // Member fields (20)
            for (int div = 0; div < 5; div++)
            {
                var di = div;
                for (int m = 0; m < 4; m++)
                {
                    var mi = m;
                    ops.Add(() => root.Divisions[di].Departments[0].Members[mi].salary = 65000 + mi * 1000);
                }
            }

            // Task fields (20)
            for (int div = 0; div < 5; div++)
            {
                var di = div;
                for (int t = 0; t < 4; t++)
                {
                    var ti = t;
                    ops.Add(() => root.Divisions[di].Departments[0].Projects[0].Tasks[ti].hours = 10 + ti);
                }
            }

            // List mutations (15)
            for (int div = 0; div < 5; div++)
            {
                var di = div;
                ops.Add(() => root.Divisions[di].Departments[0].Members.Add(new EmployeeNode { name = $"New_{di}", salary = 55000 }));
                ops.Add(() => root.Divisions[di].Departments[0].Projects[0].Tasks.Add(new TaskNode { description = $"Extra_{di}", hours = 3 }));
                ops.Add(() => root.Divisions[di].Departments[0].Members.RemoveAt(root.Divisions[di].Departments[0].Members.Count - 1));
            }

            // Dict mutations (15)
            for (int i = 0; i < 5; i++)
            {
                var key = $"med_dyn_{i}";
                ops.Add(() => root.GlobalConfig.Add(key, new ConfigNode { key = key, value = "v", version = 1 }));
                ops.Add(() => root.GlobalConfig[key].version = 2);
                ops.Add(() => root.GlobalConfig.Remove(key));
            }

            return ops.ToArray();
        }

        public static Action[] HighOps(RetreeBase rootBase)
        {
            var root = (ConglomerateNode)rootBase;
            var ops = new List<Action>();
            int c = 0;

            // Root/CEO (50)
            for (int i = 0; i < 25; i++)
            {
                var v = i;
                ops.Add(() => root.revenue = 50000000 + v * 10000);
                ops.Add(() => root.ceo.salary = 500000 + v * 100);
            }

            // Member/manager field changes (100)
            for (int div = 0; div < 5; div++)
            {
                var di = div;
                for (int d = 0; d < 4; d++)
                {
                    var dept_i = d;
                    ops.Add(() => root.Divisions[di].Departments[dept_i].manager.salary = 150000 + (++c) * 10);
                    for (int m = 0; m < 4; m++)
                    {
                        var mi = m;
                        ops.Add(() => root.Divisions[di].Departments[dept_i].Members[mi].salary = 60000 + (++c) * 10);
                    }
                }
            }

            // Task field changes (100)
            for (int div = 0; div < 5; div++)
            {
                var di = div;
                for (int d = 0; d < 4; d++)
                {
                    var dept_i = d;
                    for (int p = 0; p < 2; p++)
                    {
                        var pi = p;
                        // Clamp to available tasks (5 per project in build)
                        var taskCount = Math.Min(5, 5);
                        for (int t = 0; t < taskCount && ops.Count < 350; t++)
                        {
                            var ti = t;
                            ops.Add(() => root.Divisions[di].Departments[dept_i].Projects[pi].Tasks[ti].hours = ++c);
                        }
                    }
                }
            }

            // List add/remove cycles (100)
            for (int div = 0; div < 5; div++)
            {
                var di = div;
                for (int d = 0; d < 4; d++)
                {
                    var dept_i = d;
                    for (int i = 0; i < 5; i++)
                    {
                        var idx = i;
                        ops.Add(() => root.Divisions[di].Departments[dept_i].Members.Add(
                            new EmployeeNode { name = $"Cycle_{di}_{dept_i}_{idx}", salary = 40000 }));
                        ops.Add(() => root.Divisions[di].Departments[dept_i].Members.RemoveAt(
                            root.Divisions[di].Departments[dept_i].Members.Count - 1));
                    }
                }
            }

            // Dict mutations (100)
            for (int i = 0; i < 50; i++)
            {
                var key = $"stress_{i}";
                ops.Add(() => root.GlobalConfig.Add(key, new ConfigNode { key = key, value = "v", version = 1 }));
                ops.Add(() => root.GlobalConfig.Remove(key));
            }

            // Reference swaps (remaining to reach ~500)
            while (ops.Count < 500)
            {
                var divIdx = ops.Count % 5;
                var deptIdx = (ops.Count / 5) % 4;
                var di = divIdx;
                var dept_i = deptIdx;
                ops.Add(() => root.Divisions[di].Departments[dept_i].manager =
                    new EmployeeNode { name = $"SwapMgr_{di}_{dept_i}", salary = 160000, level = 9 });
            }

            return ops.ToArray();
        }
    }
}
