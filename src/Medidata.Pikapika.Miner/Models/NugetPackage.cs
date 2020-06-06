using NuGet.Protocol.Core.Types;

namespace Medidata.Pikapika.Miner.Models
{
    public class NugetPackage
    {
        public IPackageSearchMetadata PackageSearchMetadata { get; private set; }

        public NugetPackage(IPackageSearchMetadata packageSearchMetadata)
        {
            PackageSearchMetadata = packageSearchMetadata;
        }
    }
}
