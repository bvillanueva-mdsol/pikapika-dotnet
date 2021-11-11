using Medidata.Pikapika.Miner;
using Medidata.Pikapika.Miner.DataAccess;
using Medidata.Pikapika.Miner.Extensions;
using Medidata.Pikapika.Worker.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCrontab;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Medidata.Pikapika.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerConfiguration _workerConfiguration;

        private readonly CrontabSchedule _crontabSchedule;
        private DateTime _nextRun;


        public Worker(
            IOptions<WorkerConfiguration> workerConfigurationAccessor,
            ILogger<Worker> logger)
        {
            _workerConfiguration = workerConfigurationAccessor.Value;
            _logger = logger;

            _crontabSchedule = CrontabSchedule.Parse(_workerConfiguration.WorkerCronSchedule,
                new CrontabSchedule.ParseOptions { IncludingSeconds = true });
            _nextRun = _crontabSchedule.GetNextOccurrence(DateTime.Now);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(UntilNextExecution(), stoppingToken); // wait until next time

                await Execute(); //execute some task

                _nextRun = _crontabSchedule.GetNextOccurrence(DateTime.Now);
            }
        }

        private int UntilNextExecution() => Math.Max(0, (int)_nextRun.Subtract(DateTime.Now).TotalMilliseconds);

        private async Task Execute()
        {
            _logger.LogInformation("Pikapika-dotnet start mining...");

            try
            {
                var logger = new Logger(); // TODO, integrate with ILogger
                var dbAccess = new PikapikaRepositoryAccess(
                    _workerConfiguration.ConnectionStrings.PikapikaDatabase,
                    logger);
                var dotnetAppsMiner = new DotnetAppsMiner(
                    _workerConfiguration.AuthorizationUsername,
                    _workerConfiguration.AuthorizationToken,
                    _workerConfiguration.GithubBaseUri,
                    _workerConfiguration.MedidataRepositories,
                    logger);
                var nugetRepositoryAccess = new NugetRepositoryAccess(
                    new Uri(_workerConfiguration.PublicNugetServerUri),
                    new Uri(_workerConfiguration.MedidataNugetServerBaseUri),
                    _workerConfiguration.MedidataNugetAccessUserName,
                    _workerConfiguration.MedidataNugetAccessPassword,
                    logger);
                var dotnetNugetsMiner = new DotnetNugetsMiner(nugetRepositoryAccess, logger);

                var timer = new Stopwatch();
                timer.Start();

                // mine dotnet projects
                var dotnetAppsFromDb = await dbAccess.GetDotnetApps();
                var dotnetRepos = await dotnetAppsMiner.Mine(dotnetAppsFromDb);
                //mine dotnet nugets
                var dotnetNugetsToMine = (await dbAccess.GetDotnetNugets())
                   .Select(x => x.Name).ToList();
                dotnetNugetsToMine.AddRange(dotnetRepos
                    .SelectMany(x => x.Projects
                        .SelectMany(y => y.DotnetAppProject.ProjectNugets
                            .Select(z => z.Name))));
                var dotnetNugets = await dotnetNugetsMiner.Mine(dotnetNugetsToMine
                    .Distinct()
                    .OrderBy(x => x).ToList());
                var dotnetFrameworks = dotnetRepos
                    .SelectMany(x => x.Projects
                        .SelectMany(y => y.DotnetAppProject.Frameworks))
                    .Distinct()
                    .OrderBy(x => x).ToList();
                //save dotnet projects to db
                var newdDotnetApps = dotnetRepos.SelectMany(x => x.ConvertToDotnetApps()).ToList();
                var savedDotnetApps = await dbAccess.SaveDotnetApps(newdDotnetApps);
                // save dotnet nugets to db
                var newDotnetNugets = dotnetNugets.Values.Select(x => x.ConvertToDotnetNugets()).ToList();
                var savedDotnetNugets = await dbAccess.SaveDotnetNugets(newDotnetNugets);
                // save dotnet projects and nugets relationship to db
                var dotnetAppNugetRelationship = dotnetRepos.SelectMany(x => x.ConvertToDotnetAppDotnetNugetList(savedDotnetApps, savedDotnetNugets, logger)).ToList();
                await dbAccess.SaveDotnetAppDotnetNugetRelationships(dotnetAppNugetRelationship);

                timer.Stop();
                logger.LogInformation($"Operation elapsed time: {timer.Elapsed}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured:{ex.Message}");
            }

            _logger.LogInformation("Pikapika-dotnet mining stopped");
        }
    }
}
