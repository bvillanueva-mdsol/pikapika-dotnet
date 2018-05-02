using System.Collections.Generic;
using System.Threading.Tasks;
using Medidata.Pikapika.Miner.DataAccess;
using Medidata.Pikapika.Miner.Models;

namespace Medidata.Pikapika.Miner
{
    public class DotnetNugetsMiner
    {
        private NugetRepositoryAccess _nugetRepositoryAccess;

        public DotnetNugetsMiner(NugetRepositoryAccess nugetRepositoryAccess)
        {
            _nugetRepositoryAccess = nugetRepositoryAccess;
        }

        public async Task<IDictionary<string, IEnumerable<NugetPackage>>> Mine(IEnumerable<string> nugetIds)
        {
            var result = new Dictionary<string, IEnumerable<NugetPackage>>();

            foreach (var nugetId in nugetIds)
            {
                result.Add(nugetId, await _nugetRepositoryAccess.GetNugetFullInformation(nugetId));
            }

            return result;
        }
    }
}
