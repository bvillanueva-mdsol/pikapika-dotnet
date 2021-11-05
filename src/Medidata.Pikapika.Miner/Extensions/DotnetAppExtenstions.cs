using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseAccess;
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
                .Replace("Medidata.Cloud.", string.Empty)
                .Replace("Medidata.", string.Empty)
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
                        Version = GetVersionFroMetadatasource(projectNuget.Version, dotnetNugetsFromDBFiltered.First().GetVersions())
                    });
                }
            }

            return result;
        }

        private static string GetVersionFroMetadatasource(string appNugetVersion, IEnumerable<string> nugetMetadataVersions)
        {
            // no version specified 
            if (string.IsNullOrEmpty(appNugetVersion))
                return appNugetVersion;

            foreach (var nugetMetadataVersion in nugetMetadataVersions)
            {
                if (CompareVersionNumbers(appNugetVersion, nugetMetadataVersion))
                    return nugetMetadataVersion;
            }

            return appNugetVersion;
        }

        private static bool CompareVersionNumbers(string versionA, string versionB)
        {
            // Convert each version parts string to a list of strings
            List<string> a = versionA.ToLowerInvariant().Split('.').ToList();
            List<string> b = versionB.ToLowerInvariant().Split('.').ToList();

            // Ensure that each of the lists are the same length
            while (a.Count < b.Count) { a.Add("0"); }
            while (b.Count < a.Count) { b.Add("0"); }

            // Compare elements of each list
            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i]))
                    return false;
            }

            // If we reach this point, the versions are equal
            return true;
        }
    }
}
