using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Medidata.Pikapika.Miner;
using Medidata.Pikapika.Miner.DataAccess;
using Medidata.Pikapika.Miner.Extensions;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace Medidata.Pikapika.ConsoleRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                var configuration = builder.Build();

                var app = new CommandLineApplication
                {
                    Name = "pikapika-console"
                };

                app.OnExecute(async () =>
                {
                    Console.WriteLine("Pikapika-dotnet start mining...");
                    try
                    {
                        var dbAccess = new PikapikaRepositoryAccess(configuration.GetConnectionString("PikapikaDatabase"));
                        var dotnetAppsMiner = new DotnetAppsMiner(
                            configuration.GetSection("AuthorizationUsername").Value,
                            configuration.GetSection("AuthorizationToken").Value,
                            configuration.GetSection("GithubBaseUri").Value);
                        var nugetRepositoryAccess = new NugetRepositoryAccess(
                            new Uri(configuration.GetSection("PublicNugetServerUri").Value),
                            new Uri(configuration.GetSection("MedidataNugetServerBaseUri").Value),
                            configuration.GetSection("MedidataNugetToken").Value,
                            configuration.GetSection("MedidataNugetFeeds").GetChildren().Select(x => x.Value));
                        var dotnetNugetsMiner = new DotnetNugetsMiner(nugetRepositoryAccess);

                        var timer = new Stopwatch();
                        timer.Start();
                        var dotnetApps = await dotnetAppsMiner.Mine();
                        var dotnetNugets = await dotnetNugetsMiner.Mine(dotnetApps
                            .SelectMany(x => x.Projects
                                .SelectMany(y => y.DotnetAppProject.ProjectNugets
                                    .Select(z => z.Name)))
                            .Distinct());
                        var dotnetAppsFromDb = await dbAccess.SaveDotnetApps(dotnetApps.SelectMany(x => x.ConvertToDotnetApps()));
                        var dotnetNugetsFromDb = await dbAccess.SaveDotnetNugets(dotnetNugets.Values.Select(x => x.ConvertToDotnetNugets()).ToList());
                        await dbAccess.SaveDotnetAppDotnetNugetRelationships(dotnetApps.SelectMany(x => x.ConvertToDotnetAppDotnetNugetList(dotnetAppsFromDb, dotnetNugetsFromDb)).ToList());
                        timer.Stop();
                        Console.WriteLine($"Operation elapsed time: {timer.Elapsed}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception occured:{ex.Message}");
                    }

                    Console.WriteLine("Pikapika-dotnet mining stopped");
                    return 0;
                });

                app.Execute(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }
    }
}
