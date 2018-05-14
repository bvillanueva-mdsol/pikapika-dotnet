using System;
using System.Collections.Generic;
using System.Linq;
using Medidata.Pikapika.DatabaseAccess;
using Medidata.Pikapika.Miner.Models;

namespace Medidata.Pikapika.Miner.Extensions
{
    public static class DotnetAppExtenstions
    {
        public static IEnumerable<DotnetApps> ConvertToDotnetApps(this DotnetApp dotnetApp)
        {
            return dotnetApp.Projects
                .Select(x => new DotnetApps
                {
                    Name = x.ProjectFileName.FormatDotnetAppName(),
                    Slug = dotnetApp.Slug,
                    Repo = dotnetApp.Repository,
                    Path = x.ProjectFilePath,
                    SandboxDomain = dotnetApp.SandboxDomain,
                    Url = dotnetApp.Url,
                    DefaultBranch = dotnetApp.DefaultBranch,
                    Primary = dotnetApp.IsPrimary,
                    Deployed = dotnetApp.Deployed,
                    Deprecated = dotnetApp.Deprecated,
                    CreatedAt = dotnetApp.CreatedAt.DateTime,
                    UpdatedAt = dotnetApp.UpdatedAt.DateTime
                });
        }

        public static string FormatDotnetAppName(this string dotnetAppName)
        {
            return dotnetAppName
                .Replace(".csproj", string.Empty)
                .Replace("Medidata.Cloud.", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Medidata.", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace('.', '-');
        }

        public static IEnumerable<DotnetAppDotnetNugets> ConvertToDotnetAppDotnetNugetList(this DotnetApp dotnetApp,
            IEnumerable<DotnetApps> dotnetAppsFromDb, IEnumerable<DotnetNugets> dotnetNugetsFromDb, Logger logger)
        {
            var result = new List<DotnetAppDotnetNugets>();

            foreach (var project in dotnetApp.Projects)
            {
                var dotnetAppsFromDbFiltered = dotnetAppsFromDb
                    .Where(x =>
                        x.Repo.Equals(dotnetApp.Repository, StringComparison.OrdinalIgnoreCase) &&
                        x.Path.Equals(project.ProjectFilePath, StringComparison.OrdinalIgnoreCase));

                if (!dotnetAppsFromDbFiltered.Any())
                {
                    logger.LogWarning($"Nuget {dotnetApp.Repository}/{project.ProjectFilePath} not found in Apps Master List.");
                    continue;
                }

                foreach (var projectNuget in project.DotnetAppProject.ProjectNugets)
                {
                    var dotnetNugetsFromDBFiltered = dotnetNugetsFromDb
                        .Where(x =>
                            x.Slug.Equals(projectNuget.Name, StringComparison.OrdinalIgnoreCase));

                    if (!dotnetNugetsFromDBFiltered.Any())
                    {
                        logger.LogWarning($"Nuget {projectNuget.Name} not found in Nugets Master List.");
                        continue;
                    }

                    result.Add(new DotnetAppDotnetNugets
                    {
                        DotnetAppId = dotnetAppsFromDbFiltered.First().Id,
                        DotnetNugetId = dotnetNugetsFromDBFiltered.First().Id,
                        Version = projectNuget.Version
                    });
                }
            }

            return result;
        }
    }
}
