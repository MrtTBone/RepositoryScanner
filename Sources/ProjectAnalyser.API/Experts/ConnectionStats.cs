using System.IO;

namespace ProjectAnalyser.API.Experts
{
    public class ConnectionInfo
    {
        public string ConfigLine { get; internal set; }

        public FileInfo FileInfo { get; internal set; }
        
        public Project Project { get; internal set; }
    }
}
