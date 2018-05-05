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

            foreach (var nugetId in nugetIds)
            {
                var nugetInfo = await _nugetRepositoryAccess.GetNugetFullInformation(nugetId);
                if (nugetInfo.Any())
                {
                    result.Add(nugetId, nugetInfo);
                    continue;
                }
                _logger.LogError($"{nugetId} information cannot be found in our nuget feeds.");
            }

            return result;
        }
    }
}
