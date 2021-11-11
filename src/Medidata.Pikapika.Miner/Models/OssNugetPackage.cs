using NuGet.Protocol.Core.Types;

namespace Medidata.Pikapika.Miner.Models
{
    public class OssNugetPackage : NugetPackage
    {
        public OssNugetPackage(IPackageSearchMetadata packageSearchMetadata)
            : base(packageSearchMetadata)
        {
        }
    }
}
