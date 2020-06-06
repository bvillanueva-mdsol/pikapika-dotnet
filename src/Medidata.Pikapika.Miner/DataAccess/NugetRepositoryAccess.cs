using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Medidata.Pikapika.Miner.Models;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Medidata.Pikapika.Miner.DataAccess
{
    public class NugetRepositoryAccess
    {
        internal readonly List<(string sourceUri, PackageMetadataResource packageMetadataResource)> _sources;
        
        public NugetRepositoryAccess(Uri publicNugetServerUri,
            Uri medidataNugetServerBaseUri, string medidataNugetToken,
            IEnumerable<string> medidataNugetFeedNames)
        {
            _sources = new List<(string, PackageMetadataResource)>();

            var publicSourceRepository = new SourceRepository(new PackageSource(publicNugetServerUri.AbsoluteUri), Repository.Provider.GetCoreV3());
            var publicPackageMetadataResource = publicSourceRepository.GetResourceAsync<PackageMetadataResource>().Result;
            _sources.Add((publicSourceRepository.PackageSource.SourceUri.AbsoluteUri, publicPackageMetadataResource));

            foreach (var medidataNugetFeedName in medidataNugetFeedNames)
            {
                var medidataSourceUri = new Uri(medidataNugetServerBaseUri, $"F/{medidataNugetFeedName}/auth/{medidataNugetToken}/api/v3/index.json");
                var medidataSourceRepository = new SourceRepository(new PackageSource(medidataSourceUri.AbsoluteUri), Repository.Provider.GetCoreV3());
                var medidataPackageMetadataResource = medidataSourceRepository.GetResourceAsync<PackageMetadataResource>().Result;
                _sources.Add((medidataSourceRepository.PackageSource.SourceUri.AbsoluteUri, medidataPackageMetadataResource));
            }
        }

        public async Task<(IEnumerable<NugetPackage> packages, string foundFeedUri)> GetNugetFullInformation(string nugetId)
        {
            foreach (var nugetFeed in _sources)
            {
                var nugetSearchResult = await nugetFeed.packageMetadataResource.GetMetadataAsync(nugetId, true, false, new Logger(), CancellationToken.None);

                if (nugetSearchResult.Count() != 0)
                    return (nugetSearchResult.Select(x => new PrivateNugetPackage(x)), nugetFeed.sourceUri);
            }

            return (Enumerable.Empty<NugetPackage>(), string.Empty);
        }
    }
}
