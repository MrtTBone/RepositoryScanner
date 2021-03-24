using System.Collections.Generic;
using System.Linq;

namespace ProjectAnalyser.API.Experts
{
    public class ProjectStats
    {
        public IEnumerable<Project> Projects { get; internal set; }
        public IEnumerable<IGrouping<string, Project>> DuplicateProjects { get; internal set; }

        public IEnumerable<IGrouping<string, Project>> TargetFrameworkStats { get; internal set; }
        public IEnumerable<BrokenDependancy> BrokenDependancies { get; internal set; }
    }
}