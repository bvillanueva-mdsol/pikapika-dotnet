using System.Collections.Generic;
using Newtonsoft.Json;

namespace Medidata.Pikapika.Miner.Models
{
    public class DotnetAppProject : PikapikaBaseClass
    {
        public IEnumerable<string> Frameworks { get; set; }

        public IEnumerable<DotnetAppProjectNuget> ProjectNugets { get; set; }
    }
}
