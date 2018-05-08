using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medidata.Pikapika.Miner.DataAccess;
using Medidata.Pikapika.Miner.Models;

namespace Medidata.Pikapika.Miner
{
    public class DotnetNugetsMiner
    {
        private NugetRepositoryAccess _nugetRepositoryAccess;

        private Logger _logger;

        public DotnetNugetsMiner(NugetRepositoryAccess nugetRepositoryAccess, Logger logger)
        {
            _nugetRepositoryAccess = nugetRepositoryAccess;
            _logger = logger;
        }

        public async Task<IDictionary<string, IEnumerable<NugetPackage>>> Mine(IEnumerable<string> nugetIds)
        {
            var result = new Dictionary<string, IEnumerable<NugetPackage>>();
            var nugetFeedUsage = new Dictionary<string, int>();
            nugetFeedUsage.Add(_nugetRepositoryAccess._publicNugetServerRepository.PackageSource.SourceUri.AbsoluteUri, 0);
            foreach (var medidataNugetFeed in _nugetRepositoryAccess._medidataNugetFeeds)
            {
                nugetFeedUsage.Add(medidataNugetFeed.PackageSource.SourceUri.AbsoluteUri, 0);
            }

            foreach (var nugetId in nugetIds)
            {
                var nugetInfo = await _nugetRepositoryAccess.GetNugetFullInformation(nugetId);
                if (nugetInfo.packages.Any())
                {
                    result.Add(nugetId, nugetInfo.packages);
                    nugetFeedUsage[nugetInfo.foundFeedUri]++;
                    continue;
                }
                _logger.LogError($"{nugetId} information cannot be found in our nuget feeds.");
            }

            foreach(var nugetFeed in nugetFeedUsage)
            {
                _logger.LogDebug($"Nugget feed: {nugetFeed.Key}, Usage Count: {nugetFeed.Value}");
            }

            return result;
        }
    }
}
