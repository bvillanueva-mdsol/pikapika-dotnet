using System;
using System.Collections.Generic;

namespace Medidata.Pikapika.Miner.Models
{
    public class DotnetApp : PikapikaBaseClass
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Slug { get; set; }

        public string Repository { get; set; }

        public string SandboxDomain { get; set; }

        public string Url { get; set; }

        public string DefaultBranch { get; set; }

        public bool IsPrimary { get; set; } = false;

        public bool Deployed { get; set; } = true;

        public bool Deprecated { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public IEnumerable<DotnetAppProjectFile> Projects { get; set; }
    }
}
