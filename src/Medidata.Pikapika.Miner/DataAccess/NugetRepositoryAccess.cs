using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Medidata.Pikapika.Miner.Models;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Medidata.Pikapika.Miner.DataAccess
{
    public class NugetRepositoryAccess
    {
        internal readonly SourceRepository _publicNugetServerRepository;

        internal readonly List<SourceRepository> _medidataNugetFeeds;
        
        public NugetRepositoryAccess(Uri publicNugetServerUri,
            Uri medidataNugetServerBaseUri, string medidataNugetToken,
            IEnumerable<string> medidataNugetFeedNames)
        {
            var publicNugetServerRepository = new List<Lazy<INuGetResourceProvider>>(Repository.Provider.GetCoreV3());

            _publicNugetServerRepository = new SourceRepository(
                new NuGet.Configuration.PackageSource(publicNugetServerUri.AbsoluteUri),
                publicNugetServerRepository);
            _medidataNugetFeeds = new List<SourceRepository>();
            foreach (var medidataNugetFeedName in medidataNugetFeedNames)
            {
                var sourceUri = new Uri(medidataNugetServerBaseUri, $"F/{medidataNugetFeedName}/auth/{medidataNugetToken}/api/v3/index.json");
                _medidataNugetFeeds.Add(new SourceRepository(
                    new NuGet.Configuration.PackageSource(sourceUri.AbsoluteUri),
                    publicNugetServerRepository));
            }
        }

        public async Task<(IEnumerable<NugetPackage> packages, string foundFeedUri)> GetNugetFullInformation(string nugetId)
        {
            var publicNugetPackageMetadataResource = await _publicNugetServerRepository.GetResourceAsync<PackageMetadataResource>();
            var publicNugetSearchResult =  await publicNugetPackageMetadataResource.GetMetadataAsync(nugetId, true, false, new Logger(), CancellationToken.None);

            if (publicNugetSearchResult.Count() != 0)
                return (publicNugetSearchResult.Select(x => new OssNugetPackage(x)), _publicNugetServerRepository.PackageSource.SourceUri.AbsoluteUri);

            foreach (var medidataNugetFeed in _medidataNugetFeeds)
            {
                var medidataNugetPackageMetadataResource = await medidataNugetFeed.GetResourceAsync<PackageMetadataResource>();
                var medidataNugetSearchResult = await medidataNugetPackageMetadataResource.GetMetadataAsync(nugetId, true, false, new Logger(), CancellationToken.None);

                if (medidataNugetSearchResult.Count() != 0)
                    return (medidataNugetSearchResult.Select(x => new PrivateNugetPackage(x)), medidataNugetFeed.PackageSource.SourceUri.AbsoluteUri);
            }

            return (Enumerable.Empty<NugetPackage>(), string.Empty);
        }
    }
}
