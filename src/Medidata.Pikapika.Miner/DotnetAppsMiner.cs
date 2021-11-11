using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medidata.Pikapika.DatabaseAccess;
using Medidata.Pikapika.Miner.DataAccess;
using Medidata.Pikapika.Miner.DataAccess.Models.SearchCodeApi;
using Medidata.Pikapika.Miner.Extensions;
using Medidata.Pikapika.Miner.Models;
using Octokit;

namespace Medidata.Pikapika.Miner
{
    public class DotnetAppsMiner
    {
        private GithubAccess _githubAccess;

        private GitHubClient _githubOfficialClient;

        private IEnumerable<string> _medidataRepositories;

        private Logger _logger;

        public DotnetAppsMiner(string authorizationUsername,
            string authorizationToken, string githubBaseUri, IEnumerable<string> medidataRepositories, Logger logger)
        {
            _githubAccess = new GithubAccess(new Uri(githubBaseUri), authorizationUsername, authorizationToken, logger);
            _githubOfficialClient = new GitHubClient(new ProductHeaderValue(Helpers.Constants.UserAgent))
            {
                Credentials = new Credentials(authorizationToken)
            };
            _medidataRepositories = medidataRepositories;
            _logger = logger;
        }

        public async Task<IEnumerable<DotnetApp>> Mine(IEnumerable<DotnetApps> dotnetAppsFromDb)
        {
            // get all c# mdsol repos
            var newOrUpdatedDotnetApps = await GetNewOrUpdatedDotnetApps(TransformToRepoDatetimeDictionary(dotnetAppsFromDb));
            var count = newOrUpdatedDotnetApps.Count();
            _logger.LogInformation($"Dotnet apps count: {count}");

            // loop mdsol repos
            var counter = 0;
            foreach (var dotnetApp in newOrUpdatedDotnetApps)
            {
                var projectFiles = new List<DotnetAppProjectFile>();

                // get projects
                var githubSearchItems = await GetAllProjects(".csproj", "csproj", dotnetApp.Repository);

                // loop through the projects
                foreach (var githubSearchItem in githubSearchItems)
                {
                    var projectFile = githubSearchItem.ConvertToDotnetAppProjectFile();

                    var projectFileContent = await _githubAccess.GetFileContent(projectFile.ProjectContentsUrl);
                    var projectXmlDocument = projectFileContent.TryConvertToXDocument(_logger, out bool isProjectFileContentValid);

                    if (!isProjectFileContentValid)
                    {
                        _logger.LogInformation($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} is very old or invalid! skipping..");
                        continue;
                    }
                    else
                    {
                        if (projectXmlDocument.IsNewCsProjFormat())
                        {
                            _logger.LogInformation($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} is new!");
                            projectFile.DotnetAppProject = projectXmlDocument.ConvertToDotnetAppProject();

                            if (//!projectFile.DotnetAppProject.Frameworks.Any() &&
                                !projectFile.DotnetAppProject.ProjectNugets.Any())
                            {
                                _logger.LogInformation($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} has no frameworks/nugets! skipping..");
                                continue;
                            }

                            //var netstandardFramework = projectFile.DotnetAppProject.Frameworks.Where(x => x.StartsWith("netstandard")).FirstOrDefault();

                            foreach(var nuget in projectFile.DotnetAppProject.ProjectNugets)
                                _logger.LogDebug($"    Nuget: {nuget.Name} {nuget.Version}");
                        }
                        else
                        {
                            _logger.LogInformation($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} is old!");
                            var framework = projectXmlDocument.GetFrameworkFromOldCsProj();
                            projectFile.DotnetAppProject = new DotnetAppProject
                            {
                                Frameworks = framework == null ? Enumerable.Empty<string>(): new string[] { framework },
                                ProjectNugets = await GetOldCsProjProjectNugets(dotnetApp, projectFile, projectFileContent)
                            };

                            if (//!projectFile.DotnetAppProject.Frameworks.Any() &&
                                !projectFile.DotnetAppProject.ProjectNugets.Any())
                            {
                                _logger.LogInformation($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} has no nugets! skipping..");
                                continue;
                            }

                            foreach (var nuget in projectFile.DotnetAppProject.ProjectNugets)
                                _logger.LogDebug($"    Nuget: {nuget.Name} {nuget.Version}");
                        }
                    }

                    projectFiles.Add(projectFile);
                }

                dotnetApp.Projects = projectFiles;
                counter++;
                _logger.LogInformation($"Fetched projects of {dotnetApp.Repository}. {counter} of {count} Apps.");
            }

            return newOrUpdatedDotnetApps;
        }

        private static Dictionary<string, DateTime> TransformToRepoDatetimeDictionary(IEnumerable<DotnetApps> dotnetAppsFromDb)
        {
            var distinctRepos = dotnetAppsFromDb
                .Select(x => x.Repo)
                .Distinct();

            var result = new Dictionary<string, DateTime>();

            foreach (var distinctRepo in distinctRepos)
            {
                result.Add(distinctRepo, dotnetAppsFromDb
                    .Where(x => x.Repo == distinctRepo)
                    .Select(x => x.UpdatedAt)
                    .OrderBy(x => x)
                    .First());
            }

            return result;
        }

        private async Task<IEnumerable<DotnetApp>> GetNewOrUpdatedDotnetApps(Dictionary<string, DateTime> repoDatetimeDictionary)
        {
            var allDotnetApps = (await _githubOfficialClient.Repository.GetAllForOrg("mdsol"))
                .Where(x => x.Language == "C#")
                .OrderByDescending(x => x.PushedAt)
                .Select(cSharpRepo => new DotnetApp
                {
                    Name = cSharpRepo.Name,
                    Slug = cSharpRepo.FullName,
                    Repository = cSharpRepo.FullName,
                    Url = cSharpRepo.Url,
                    DefaultBranch = cSharpRepo.DefaultBranch,
                    CreatedAt = cSharpRepo.CreatedAt,
                    UpdatedAt = cSharpRepo.PushedAt.Value
                });

            var result = allDotnetApps
                //.Where(app => _medidataRepositories.Any(medRep => medRep.Equals(app.Repository, StringComparison.OrdinalIgnoreCase)))
                .Where(app =>
                    !repoDatetimeDictionary.Any(x =>
                        x.Key.Equals(app.Repository, StringComparison.OrdinalIgnoreCase)) ||
                    repoDatetimeDictionary.Any(x =>
                        x.Key.Equals(app.Repository, StringComparison.OrdinalIgnoreCase) &&
                        !x.Value.Equals(app.UpdatedAt.DateTime)))
                .ToList();

            int tempcount = 0;
            foreach (var app in result)
            {
                tempcount++;
                _logger.LogInformation($"#{tempcount} Repo Name: {app.Repository}, UpdatedAt: {app.UpdatedAt} ");
            }

            return result;
        }

        private async Task<IEnumerable<ResultItem>> GetAllProjects(string projectQuery, string extension, string repo)
        {
            return (await _githubAccess.SearchDotnetFiles(projectQuery, extension, repo))
                    .Where(item => item.Path.EndsWith(projectQuery, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<ResultItem> SearchFile(string projectQuery, string extension, string repo, string path)
        {
            return (await _githubAccess.SearchDotnetFiles(projectQuery, extension, repo, path))
                    .Where(item => item.Path.Equals($"{path}/{projectQuery}", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
        }

        private async Task<IEnumerable<DotnetAppProjectNuget>> GetOldCsProjProjectNugets(DotnetApp dotnetApp,
            DotnetAppProjectFile projectFile, string projectFileContent)
        {
            var query = "packages.config";
            if (!projectFileContent.Contains(query))
            {
                _logger.LogWarning($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} has no packages.config file in csproj!");
                return Enumerable.Empty<DotnetAppProjectNuget>();
            }

            var packagesConfigFile = await SearchFile(
                            "packages.config",
                            "config",
                            dotnetApp.Repository,
                            projectFile.ProjectFilePath.Replace($"/{projectFile.ProjectFileName}", string.Empty));

            if (packagesConfigFile == null)
            {
                _logger.LogWarning($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} has no packages.config file in repo!");
                return Enumerable.Empty<DotnetAppProjectNuget>();
            }

            var packagesConfigContent = await _githubAccess.GetFileContent(packagesConfigFile.ContentsUrl);
            var packagesConfigXmlDocument = packagesConfigContent.TryConvertToXDocument(_logger, out bool isPackagesConfigFileContentValid);
            if (!isPackagesConfigFileContentValid)
            {
                _logger.LogError($"{dotnetApp.Repository}/{packagesConfigFile.Path} is not valid!");
                return Enumerable.Empty<DotnetAppProjectNuget>();
            }

            return packagesConfigXmlDocument.GetPackagesConfigReferences();
        }
    }
}
