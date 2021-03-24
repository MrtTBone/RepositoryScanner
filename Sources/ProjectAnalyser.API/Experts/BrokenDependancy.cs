using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectAnalyser.API.Experts
{
    public class BrokenDependancy
    {
        public Project Project { get; internal set; }
        public List<KeyValuePair<string, Project>> BrokenDependancies { get; internal set; }
    }
}
