using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Medidata.Pikapika.Miner.DataAccess;
using Medidata.Pikapika.Miner.Models;
using NuGet.Protocol.Core.Types;

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

    public class Logger : NuGet.Common.ILogger
    {
        public void LogDebug(string data) => Console.WriteLine($"DEBUG: {data}");
        public void LogVerbose(string data) => Console.WriteLine($"VERBOSE: {data}");
        public void LogInformation(string data) => Console.WriteLine($"INFORMATION: {data}");
        public void LogMinimal(string data) => Console.WriteLine($"MINIMAL: {data}");
        public void LogWarning(string data) => Console.WriteLine($"WARNING: {data}");
        public void LogError(string data) => Console.WriteLine($"ERROR: {data}");
        public void LogSummary(string data) => Console.WriteLine($"SUMMARY: {data}");

        public void LogInformationSummary(string data)
        {
            Console.WriteLine($"Information Summary: {data}");
        }

        public void LogErrorSummary(string data)
        {
            Console.WriteLine($"Error Summary: {data}");
        }
    }
}
