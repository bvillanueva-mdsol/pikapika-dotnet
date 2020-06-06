using NuGet.Protocol.Core.Types;

namespace Medidata.Pikapika.Miner.Models
{
    public class PrivateNugetPackage : NugetPackage
    {
        public PrivateNugetPackage(IPackageSearchMetadata packageSearchMetadata)
            : base(packageSearchMetadata)
        {
        }
    }
}
