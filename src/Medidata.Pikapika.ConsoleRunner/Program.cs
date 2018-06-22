using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Medidata.Pikapika.Miner;
using Medidata.Pikapika.Miner.DataAccess;
using Medidata.Pikapika.Miner.Extensions;
using Medidata.Pikapika.Miner.Helpers;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace Medidata.Pikapika.ConsoleRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new Logger();
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configuration = builder.Build();

            var app = new CommandLineApplication
            {
                Name = Constants.UserAgent
            };

            app.OnExecute(async () =>
            {
                logger.LogInformation("Pikapika-dotnet start mining...");

                try
                {
                    var dbAccess = new PikapikaRepositoryAccess(
                        configuration.GetConnectionString("PikapikaDatabase"),
                        logger);
                    var dotnetAppsMiner = new DotnetAppsMiner(
                        configuration.GetSection("AuthorizationUsername").Value,
                        configuration.GetSection("AuthorizationToken").Value,
                        configuration.GetSection("GithubBaseUri").Value,
                         configuration.GetSection("MedidataRepositories").GetChildren().Select(x => x.Value),
                        logger);
                    var nugetRepositoryAccess = new NugetRepositoryAccess(
                        new Uri(configuration.GetSection("PublicNugetServerUri").Value),
                        new Uri(configuration.GetSection("MedidataNugetServerBaseUri").Value),
                        configuration.GetSection("MedidataNugetToken").Value,
                        configuration.GetSection("MedidataNugetFeeds").GetChildren().Select(x => x.Value));
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
                    logger.LogError($"Exception occured:{ex.Message}");
                }

                logger.LogInformation("Pikapika-dotnet mining stopped");

                //Console.ReadLine();
                return 0;
            });

            app.Execute(args);

            //Console.ReadLine();
        }
    }
}
