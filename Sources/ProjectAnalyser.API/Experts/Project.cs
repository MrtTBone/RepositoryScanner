using System;
using System.Collections.Generic;

namespace ProjectAnalyser.API.Experts
{
    public class Project
    {
        public Project()
        {
            this.Id = Guid.NewGuid();
        }
        public Guid Id { get; }
        public string Name { get; internal set; }
        public string FullPath { get; internal set; }
        public string TargetFramework { get; internal set; }
        public bool IsOldFormat { get; internal set; }
        public IEnumerable<string> Dependancies { get; internal set; }
        public IEnumerable<string> ProjectDependancies { get; internal set; }
        public IDictionary<string, Project> ProjectDependanciesExpanded { get; internal set; }
        public List<Solution> Solutions { get; } = new List<Solution>();
        public IEnumerable<Project> ReferenceBy { get; internal set; }
        public IEnumerable<ConnectionInfo> Configurations { get; internal set; }
    }
}