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
                    Name = $"{dotnetApp.Name}/{x.ProjectFileName.Replace(".csproj", string.Empty)}",
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

        public static IEnumerable<DotnetAppDotnetNugets> ConvertToDotnetAppDotnetNugetList(this DotnetApp dotnetApp, IEnumerable<DotnetApps> dotnetAppsFromDb, IEnumerable<DotnetNugets> dotnetNugetsFromDb)
        {
            var result = new List<DotnetAppDotnetNugets>();

            foreach (var project in dotnetApp.Projects)
            {
                var dotnetAppId = dotnetAppsFromDb
                    .Where(x =>
                        x.Repo.Equals(dotnetApp.Repository, StringComparison.OrdinalIgnoreCase) &&
                        x.Path.Equals(project.ProjectFilePath, StringComparison.OrdinalIgnoreCase))
                    .First().Id;

                foreach (var projectNuget in project.DotnetAppProject.ProjectNugets)
                {
                    var dotnetNugetId = dotnetNugetsFromDb
                        .Where(x =>
                            x.Slug.Equals(projectNuget.Name, StringComparison.OrdinalIgnoreCase))
                        .First().Id;

                    result.Add(new DotnetAppDotnetNugets
                    {
                        DotnetAppId = dotnetAppId,
                        DotnetNugetId = dotnetNugetId,
                        Version = projectNuget.Version
                    });
                }
            }

            return result;
        }
    }
}
