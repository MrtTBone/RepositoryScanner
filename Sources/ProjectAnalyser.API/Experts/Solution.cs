using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectAnalyser.API.Experts
{
    public class Solution
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string FullPath { get; set; }

        public Dictionary<string, Project> Projects { get; internal set; }
    }
}
