using System;
using System.Collections.Generic;

namespace Medidata.Pikapika.DatabaseAccess
{
    public partial class DotnetApps
    {
        public DotnetApps()
        {
            DotnetAppDotnetNugets = new HashSet<DotnetAppDotnetNugets>();
        }

        public long Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Repo { get; set; }
        public string Path { get; set; }
        public string SandboxDomain { get; set; }
        public string Url { get; set; }
        public string DefaultBranch { get; set; }
        public bool? Primary { get; set; }
        public bool? Deployed { get; set; }
        public bool? Deprecated { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<DotnetAppDotnetNugets> DotnetAppDotnetNugets { get; set; }
    }
}
