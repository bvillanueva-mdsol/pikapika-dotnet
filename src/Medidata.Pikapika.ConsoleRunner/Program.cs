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

                        var timer = new Stopwatch();
                        timer.Start();
                        var dotnetApps = await dotnetAppsMiner.Mine();
                        await dbAccess.PushData(dotnetApps.SelectMany(x => x.ConvertToDotnetAppProjectFile()));
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
