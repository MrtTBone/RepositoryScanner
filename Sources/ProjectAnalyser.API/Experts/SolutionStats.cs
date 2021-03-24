using System.Collections.Generic;
using System.Linq;

namespace ProjectAnalyser.API.Experts
{
    public class SolutionStats
    {
        public IEnumerable<IGrouping<string, Solution>> DuplicateSolutions { get; internal set; }

        public IEnumerable<Solution> Solutions { get; internal set; }
    }
}