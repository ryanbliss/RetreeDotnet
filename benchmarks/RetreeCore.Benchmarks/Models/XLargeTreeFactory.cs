// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace RetreeCore.Benchmarks.Models
{
    /// <summary>
    /// Builds an extra-large tree (~5,000 nodes, depth 5) reusing the Large node types.
    /// Structure: ConglomerateNode → 10 divisions → 8 departments each
    ///   → 12 members + 4 projects (8 tasks + 3 metadata each) per department
    ///   + 20 global config entries
    /// </summary>
    public static class XLargeTreeFactory
    {
        public static ConglomerateNode Build()
        {
            var root = new ConglomerateNode
            {
                name = "MegaCorp Global",
                revenue = 500000000,
                employeeCount = 5000,
                publiclyTraded = true,
                ceo = new EmployeeNode { name = "CEO Global", salary = 1000000, level = 20 }
            };

            var regions = new[]
            {
                "North America", "Europe", "Asia Pacific", "Latin America", "Africa",
                "Middle East", "South Asia", "Central Europe", "Oceania", "Scandinavia"
            };

            for (int div = 0; div < 10; div++)
            {
                var division = new DivisionNode
                {
                    name = $"Division_{div}",
                    budget = 10000000 + div * 2000000,
                    region = regions[div],
                    vp = new EmployeeNode { name = $"VP_{div}", salary = 400000, level = 14 }
                };

                for (int d = 0; d < 8; d++)
                {
                    var dept = new LargeDepartmentNode
                    {
                        name = $"Dept_{div}_{d}",
                        budget = 1000000 + d * 100000,
                        headcount = 12,
                        manager = new EmployeeNode { name = $"Mgr_{div}_{d}", salary = 180000, level = 9 }
                    };

                    for (int m = 0; m < 12; m++)
                    {
                        dept.Members.Add(new EmployeeNode
                        {
                            name = $"Emp_{div}_{d}_{m}",
                            salary = 55000 + m * 3000,
                            level = (m % 5) + 1,
                            active = true
                        });
                    }

                    for (int p = 0; p < 4; p++)
                    {
                        var project = new LargeProjectNode
                        {
                            title = $"Proj_{div}_{d}_{p}",
                            priority = (p % 3) + 1,
                            storyPoints = 10 + p * 8,
                            complete = false
                        };

                        for (int t = 0; t < 8; t++)
                        {
                            project.Tasks.Add(new TaskNode
                            {
                                description = $"Task_{div}_{d}_{p}_{t}",
                                hours = 2 + t * 3,
                                done = false
                            });
                        }

                        for (int md = 0; md < 3; md++)
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

            for (int gc = 0; gc < 20; gc++)
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

        /// <summary>
        /// ~10,000 operations across all tree levels with a full mix of operation types.
        /// </summary>
        public static Action[] XHighOps(RetreeBase rootBase)
        {
            var root = (ConglomerateNode)rootBase;
            var ops = new List<Action>(10500);
            int c = 0;

            // ===== Root/CEO field changes (~500) =====
            for (int i = 0; i < 125; i++)
            {
                var v = i;
                ops.Add(() => root.revenue = 500000000 + v * 10000);
                ops.Add(() => root.ceo.salary = 1000000 + v * 100);
                ops.Add(() => root.employeeCount = 5000 + v);
                ops.Add(() => root.ceo.level = 20 + (v % 5));
            }

            // ===== Division field changes (~500) =====
            for (int div = 0; div < 10; div++)
            {
                var di = div;
                for (int i = 0; i < 25; i++)
                {
                    var v = i;
                    ops.Add(() => root.Divisions[di].budget = 10000000 + v * 10000);
                    ops.Add(() => root.Divisions[di].vp.salary = 400000 + v * 100);
                }
            }

            // ===== Department field changes (~800) =====
            for (int div = 0; div < 10; div++)
            {
                var di = div;
                for (int d = 0; d < 8; d++)
                {
                    var dept_i = d;
                    for (int i = 0; i < 5; i++)
                    {
                        var v = i;
                        ops.Add(() => root.Divisions[di].Departments[dept_i].budget = 1000000 + v * 5000);
                        ops.Add(() => root.Divisions[di].Departments[dept_i].headcount = 12 + v);
                    }
                }
            }

            // ===== Member field changes (~2400) =====
            for (int div = 0; div < 10; div++)
            {
                var di = div;
                for (int d = 0; d < 8; d++)
                {
                    var dept_i = d;
                    // Manager salary
                    ops.Add(() => root.Divisions[di].Departments[dept_i].manager.salary = 180000 + (++c) * 10);
                    // Each of 12 members: salary + level
                    for (int m = 0; m < 12; m++)
                    {
                        var mi = m;
                        ops.Add(() => root.Divisions[di].Departments[dept_i].Members[mi].salary = 55000 + (++c) * 10);
                        // Every other member also gets a level change
                        if (m % 2 == 0)
                        {
                            ops.Add(() => root.Divisions[di].Departments[dept_i].Members[mi].level = (c % 10) + 1);
                        }
                    }
                }
            }

            // ===== Task field changes (~2560) =====
            for (int div = 0; div < 10; div++)
            {
                var di = div;
                for (int d = 0; d < 8; d++)
                {
                    var dept_i = d;
                    for (int p = 0; p < 4; p++)
                    {
                        var pi = p;
                        for (int t = 0; t < 8; t++)
                        {
                            var ti = t;
                            ops.Add(() => root.Divisions[di].Departments[dept_i].Projects[pi].Tasks[ti].hours = ++c);
                        }
                    }
                }
            }

            // ===== Project field changes (~320) =====
            for (int div = 0; div < 10; div++)
            {
                var di = div;
                for (int d = 0; d < 8; d++)
                {
                    var dept_i = d;
                    for (int p = 0; p < 4; p++)
                    {
                        var pi = p;
                        ops.Add(() => root.Divisions[di].Departments[dept_i].Projects[pi].storyPoints = ++c);
                    }
                }
            }

            // ===== List add/remove cycles (~1600) =====
            for (int div = 0; div < 10; div++)
            {
                var di = div;
                for (int d = 0; d < 8; d++)
                {
                    var dept_i = d;
                    // Member add/remove cycles (10 per department = 10×2 = 20 ops)
                    for (int i = 0; i < 10; i++)
                    {
                        var idx = i;
                        ops.Add(() => root.Divisions[di].Departments[dept_i].Members.Add(
                            new EmployeeNode { name = $"Temp_{di}_{dept_i}_{idx}", salary = 40000 }));
                        ops.Add(() => root.Divisions[di].Departments[dept_i].Members.RemoveAt(
                            root.Divisions[di].Departments[dept_i].Members.Count - 1));
                    }
                }
            }

            // ===== Dict mutations (~1200) =====
            // Global config add/modify/remove cycles
            for (int i = 0; i < 200; i++)
            {
                var key = $"xstress_{i}";
                ops.Add(() => root.GlobalConfig.Add(key, new ConfigNode { key = key, value = "v", version = 1 }));
                ops.Add(() => root.GlobalConfig[key].version = 2);
                ops.Add(() => root.GlobalConfig.Remove(key));
            }

            // Project metadata mutations
            for (int div = 0; div < 10; div++)
            {
                var di = div;
                for (int d = 0; d < 4; d++) // first 4 departments
                {
                    var dept_i = d;
                    var metaKey = $"xmeta_{di}_{dept_i}";
                    ops.Add(() => root.Divisions[di].Departments[dept_i].Projects[0].Metadata.Add(
                        metaKey, new ConfigNode { key = metaKey, value = "new", version = 1 }));
                    ops.Add(() => root.Divisions[di].Departments[dept_i].Projects[0].Metadata[metaKey].value = "updated");
                    ops.Add(() => root.Divisions[di].Departments[dept_i].Projects[0].Metadata.Remove(metaKey));
                }
            }

            // ===== Reference swaps (~remaining to reach 10,000) =====
            while (ops.Count < 10000)
            {
                var divIdx = ops.Count % 10;
                var deptIdx = (ops.Count / 10) % 8;
                var di = divIdx;
                var dept_i = deptIdx;
                ops.Add(() => root.Divisions[di].Departments[dept_i].manager =
                    new EmployeeNode { name = $"SwapMgr_{di}_{dept_i}_{c++}", salary = 190000, level = 10 });
            }

            return ops.ToArray();
        }
    }
}
