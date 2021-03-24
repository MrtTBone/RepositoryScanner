using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectAnalyser.API.Experts
{
    public interface IScanner
    {
        Task<(ProjectStats, SolutionStats)> ScanProjects(string folder);
    }
}