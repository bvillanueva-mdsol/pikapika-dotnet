namespace Medidata.Pikapika.Miner.Models
{
    public class DotnetAppProjectFile : PikapikaBaseClass
    {
        public int Id { get; set; }

        public string ProjectFilePath { get; set; }

        public string ProjectFileName { get; set; }

        public string ProjectContentsUrl { get; set; }

        public DotnetAppProject DotnetAppProject { get; set; }
    }
}
