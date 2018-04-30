namespace Medidata.Pikapika.DatabaseAccess
{
    public partial class DotnetAppDotnetNugets
    {
        public long Id { get; set; }
        public long? DotnetAppId { get; set; }
        public long? DotnetNugetId { get; set; }
        public string Version { get; set; }
        public string Ref { get; set; }

        public DotnetApps DotnetApp { get; set; }
        public DotnetNugets DotnetNuget { get; set; }
    }
}
