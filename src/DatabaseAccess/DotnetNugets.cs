using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseAccess
{
    public partial class DotnetNugets
    {
        public DotnetNugets()
        {
            DotnetAppDotnetNugets = new HashSet<DotnetAppDotnetNugets>();
        }

        public long Id { get; set; }

        public string Name { get; set; }

        public string Slug { get; set; }

        public string Repo { get; set; }

        public string Url { get; set; }

        public string DefaultBranch { get; set; }

        public string Gemspec { get; set; }

        public string Versions { get; set; }

        public bool? Deprecated { get; set; }

        public bool? Published { get; set; }

        public bool? Oss { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public ICollection<DotnetAppDotnetNugets> DotnetAppDotnetNugets { get; set; }

        public IEnumerable<string> GetVersions()
        {
            try
            {
                var data = JsonConvert.DeserializeObject<IEnumerable<NugetVersion>>(Versions);
                return data.Select(d => d.Version).ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    public class NugetVersion
    {
        public string Version { get; set; }

        public string Timestamp { get; set; }

        public IEnumerable<FrameworkDependency> FrameworkDependencies { get; set; }
    }

    public class FrameworkDependency
    {
        public string Version { get; set; }

        public string Framework { get; set; }
    }
}
