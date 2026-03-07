// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

namespace RetreeCore.Benchmarks.Models
{
    public class EmployeeNode : RetreeNode
    {
        public string name = "";
        public int salary = 0;
        public int level = 1;
        public bool active = true;
    }

    public class TaskNode : RetreeNode
    {
        public string description = "";
        public int hours = 0;
        public bool done = false;
    }

    public class ConfigNode : RetreeNode
    {
        public string key = "";
        public string value = "";
        public int version = 1;
    }
}
