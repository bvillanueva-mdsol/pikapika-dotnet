using System;

namespace Medidata.Pikapika.Miner.Models
{
    public class DotnetNuget : PikapikaBaseClass
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Slug { get; set; }

        public string Repository { get; set; }

        public string Url { get; set; }

        public string DefaultBranch { get; set; }

        public DotnetNugetVersion Versions { get; set; }

        public bool Deprecated { get; set; } = false;

        public bool Oss { get; set; } = false;

        public bool Published { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
