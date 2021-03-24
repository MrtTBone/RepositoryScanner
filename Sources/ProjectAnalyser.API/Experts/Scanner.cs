using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace ProjectAnalyser.API.Experts
{
    public class Scanner : IScanner
    {
        private const string MSBUILSNAMESPACE = "msbld";

        public async Task<(ProjectStats, SolutionStats)> ScanProjects(string folder)
        {
            var taskProject = Task.Run(() => this.GetProjects(folder));
            var taskSolution = Task.Run(() => this.GetSolution(folder));

            var projectStat = await taskProject;
            var solutionStat = await taskSolution;
            //var environnementFront = new[] { "environnement.ts", "environnement.*.ts" };

            this.Consolidate(projectStat, solutionStat);

            return (projectStat, solutionStat);
        }

        private void Consolidate(ProjectStats projectStat, SolutionStats solutionStat)
        {
            projectStat.Projects.ToList().ForEach(x => x.ReferenceBy = projectStat.Projects.Where(y => y.ProjectDependancies.Contains(x.FullPath)));

            solutionStat.Solutions.ToList().ForEach(x => x.Projects.Keys.ToList().ForEach(y =>
            {
                x.Projects[y] = projectStat.Projects.FirstOrDefault(z => string.Equals(y, z.FullPath, StringComparison.InvariantCultureIgnoreCase));
                x.Projects[y]?.Solutions.Add(x);
            }));
        }

        private IEnumerable<ConnectionInfo> GetConfiguration(string folder)
        {
            var configFile = new[] { "web.config", "web.*.config", "appsettings.json", "appsettings.*.json" };

            return configFile.SelectMany(x => Directory.EnumerateFiles(folder, x, new EnumerationOptions { RecurseSubdirectories = true }).Select(x => new FileInfo(x)))
                .SelectMany(x => this.ScanForConnection(x));
        }

        private IEnumerable<ConnectionInfo> ScanForConnection(FileInfo fileInfo)
        {
            var connectionString = new[] { "Data Source=", "Server=", "http://", "https://", "ftp://", "sftp://", "ftps://" };
            var ignore = new[] { "http://schemas.microsoft.com/XML-Document-Transform", "http://go.microsoft.com", "http://schemas.microsoft.com" };

            return File.ReadAllLines(fileInfo.FullName).Where(line => connectionString.Any(x => 
            line.Contains(x, StringComparison.InvariantCultureIgnoreCase))
            && ignore.All(x => !line.Contains(x, StringComparison.InvariantCultureIgnoreCase))).Select(x => new ConnectionInfo() { ConfigLine = x, FileInfo = fileInfo });
        }

        private SolutionStats GetSolution(string folder)
        {
            var files = Directory.EnumerateFiles(folder, "*.sln", new EnumerationOptions { RecurseSubdirectories = true }).Select(x => new FileInfo(x));
            var solutions = files.Select(x => this.ScanSolution(x)).ToList();
            return new SolutionStats
            {
                Solutions = solutions,
                DuplicateSolutions = solutions.GroupBy(x => x.Name).Where(x => x.Count() > 1),
            };
        }

        private Solution ScanSolution(FileInfo file)
        {
            var lines = File.ReadAllLines(file.FullName);
            var projects = this.GetProjectPath(file.DirectoryName, lines.Where(x => x.StartsWith("Project")));
            return new Solution
            {
                FullPath = file.FullName,
                Name = file.Name,
                Projects = projects.ToDictionary<string, string, Project>(x => x, x => null),
                Version = lines.FirstOrDefault(x => x.StartsWith("VisualStudioVersion"))?.Split(" = ")[1].Trim(),
            };
        }

        private IEnumerable<string> GetProjectPath(string basePath, IEnumerable<string> projectLines)
        {
            return projectLines.Select(x => x.Split('=', ',').Select(y => y.Trim()).FirstOrDefault(y => y.Contains(".csproj\""))).Where(y => y != null).Select(x => GetFullPath(x.Substring(1, x.Length - 2), basePath));
        }

        private ProjectStats GetProjects(string folder)
        {
            var files = Directory.EnumerateFiles(folder, "*.csproj", new EnumerationOptions { RecurseSubdirectories = true }).Select(x => new FileInfo(x));
            var projects = files.Select(x => this.ScanProject(x)).ToList();
            projects.ForEach(x =>
            {
                x.ProjectDependanciesExpanded = x.ProjectDependancies.ToDictionary(y => y, y => this.GetProject(x, y, projects));
                x.Configurations = GetConfiguration(Path.GetDirectoryName(x.FullPath));
            });

            return new ProjectStats
            {
                Projects = projects,
                DuplicateProjects = projects.GroupBy(x => x.Name).Where(x => x.Count() > 1),
                TargetFrameworkStats = projects.GroupBy(x => x.TargetFramework).Where(x => x.Count() > 1),
                BrokenDependancies = projects.Select(x => new BrokenDependancy { Project = x, BrokenDependancies = x.ProjectDependanciesExpanded.Where(y => y.Value == null).ToList() }).Where(x => x.BrokenDependancies.Any()).ToList()
            };
        }

        private Project GetProject(Project x, string y, List<Project> projects)
        {
            var baseProjectPath = Path.GetDirectoryName(x.FullPath);

            var fullPath = GetFullPath(y, baseProjectPath);
            return projects.FirstOrDefault(x => string.Equals(x.FullPath, fullPath, StringComparison.InvariantCultureIgnoreCase));
        }

        private static string GetFullPath(string y, string baseProjectPath)
        {
            return Path.IsPathFullyQualified(y) ? y : Path.GetFullPath(Path.Combine(baseProjectPath, y));
        }

        private Project ScanProject(FileInfo file)
        {
            var doc = new XmlDocument();
            doc.Load(file.FullName);

            var result = new Project { FullPath = file.FullName, Name = file.Name };

            result.TargetFramework = GetFrameworkVersions(doc, out var isOldFormat);
            result.IsOldFormat = isOldFormat;
            if (result.IsOldFormat)
            {
                var ns = GetoldFormatNameSpace(doc);
                result.Dependancies = doc.SelectNodes($"//{MSBUILSNAMESPACE}:Reference", ns).OfType<XmlNode>().Select(x => x.Attributes["Include"]?.Value).Where(x => x != null).ToList();
                result.ProjectDependancies = doc.SelectNodes($"//{MSBUILSNAMESPACE}:ProjectReference", ns).OfType<XmlNode>().Select(x => x.Attributes["Include"]?.Value).Where(x => x != null).Select(x => GetFullPath(x, file.DirectoryName)).ToList();
            }
            else
            {
                result.Dependancies = doc.SelectNodes($"//PackageReference").OfType<XmlNode>().Select(x => x.Attributes["Include"]?.Value + (" Version=" + x.Attributes["Version"]?.Value ?? string.Empty)).Where(x => x != null).ToList();
                result.ProjectDependancies = doc.SelectNodes($"//ProjectReference").OfType<XmlNode>().Select(x => x.Attributes["Include"]?.Value).Where(x => x != null).Select(x => GetFullPath(x, file.DirectoryName)).ToList();
            }

            return result;
        }

        private static string GetFrameworkVersions(XmlDocument doc, out bool isOldFormat)
        {
            var targetFramework = doc.SelectSingleNode("//TargetFramework")?.InnerText ?? doc.SelectSingleNode("//TargetFrameworks")?.InnerText;
            if (string.IsNullOrEmpty(targetFramework))
            {
                targetFramework = doc.SelectSingleNode($"//{MSBUILSNAMESPACE}:TargetFrameworkVersion", GetoldFormatNameSpace(doc))?.InnerText;
                isOldFormat = !string.IsNullOrEmpty(targetFramework);
            }
            else
            {
                isOldFormat = false;
            }

            return targetFramework;
        }

        private static XmlNamespaceManager GetoldFormatNameSpace(XmlDocument doc)
        {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace(MSBUILSNAMESPACE, "http://schemas.microsoft.com/developer/msbuild/2003");
            return ns;
        }
    }
}
