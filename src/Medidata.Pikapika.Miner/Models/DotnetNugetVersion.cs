using System;

namespace Medidata.Pikapika.Miner.Models
{
    public class DotnetNugetVersion : PikapikaBaseClass
    {
        public int Id { get; set; }

        public string Version { get; set; }

        public string Slug { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
