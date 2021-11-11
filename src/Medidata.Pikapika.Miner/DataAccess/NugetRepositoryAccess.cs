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

        private readonly SourceCacheContext _cache = new();
        private readonly Logger _logger;

        public NugetRepositoryAccess(Uri publicNugetServerUri,
            Uri medidataNugetServerBaseUri, string medidataNugetAccessUserName,
            string medidataNugetAccessPassword, Logger logger)
        {
            _sources = new List<(string, PackageMetadataResource)>();

            var publicSourceRepository = Repository.Factory.GetCoreV3(publicNugetServerUri.AbsoluteUri);
            var publicPackageMetadataResource = publicSourceRepository.GetResourceAsync<PackageMetadataResource>().Result;
            _sources.Add((publicSourceRepository.PackageSource.SourceUri.AbsoluteUri, publicPackageMetadataResource));

            var privateSourceUri = medidataNugetServerBaseUri.AbsoluteUri;
            var privatePackageSource = new PackageSource(privateSourceUri)
            {
                Credentials = new PackageSourceCredential(
                    source: privateSourceUri,
                    username: medidataNugetAccessUserName,
                    passwordText: medidataNugetAccessPassword,
                    isPasswordClearText: true,
                    validAuthenticationTypesText: null)
            };
            var privateRepository = Repository.Factory.GetCoreV3(privatePackageSource);
            var privatePackageMetadataResource = privateRepository.GetResourceAsync<PackageMetadataResource>().Result;
            _sources.Add((privateRepository.PackageSource.SourceUri.AbsoluteUri, privatePackageMetadataResource));


            _logger = logger;
        }

        public async Task<(IEnumerable<NugetPackage> packages, string foundFeedUri)> GetNugetFullInformation(string nugetId)
        {
            foreach (var (sourceUri, packageMetadataResource) in _sources)
            {
                var nugetSearchResult = await packageMetadataResource.GetMetadataAsync(nugetId, true, false, _cache, _logger, CancellationToken.None);

                if (nugetSearchResult.Any())
                    return (nugetSearchResult.Select(x => new PrivateNugetPackage(x)), sourceUri);
            }

            return (Enumerable.Empty<NugetPackage>(), string.Empty);
        }
    }
}
