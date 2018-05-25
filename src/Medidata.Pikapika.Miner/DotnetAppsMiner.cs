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

        private Logger _logger;

        public DotnetAppsMiner(string authorizationUsername,
            string authorizationToken, string githubBaseUri, Logger logger)
        {
            _githubAccess = new GithubAccess(new Uri(githubBaseUri), authorizationUsername, authorizationToken, logger);
            _githubOfficialClient = new GitHubClient(new ProductHeaderValue(Helpers.Constants.UserAgent))
            {
                Credentials = new Credentials(authorizationToken)
            };
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
                .Select(cSharpRepo => new DotnetApp
                {
                    Name = cSharpRepo.Name,
                    Slug = cSharpRepo.FullName,
                    Repository = cSharpRepo.FullName,
                    Url = cSharpRepo.Url,
                    DefaultBranch = cSharpRepo.DefaultBranch,
                    CreatedAt = cSharpRepo.CreatedAt,
                    UpdatedAt = cSharpRepo.UpdatedAt
                });

            return allDotnetApps
                .Where(app =>
                    (
                    AreEquivalent("mdsol/Rave", app.Repository) ||
                    AreEquivalent("mdsol/meds_ingestor", app.Repository) ||
                    AreEquivalent("mdsol/coder", app.Repository) ||
                    AreEquivalent("mdsol/Balance-Almac-Drug-Shipping", app.Repository) ||
                    AreEquivalent("mdsol/meds_extractor_jobmanager", app.Repository) ||
                    AreEquivalent("mdsol/meds_extractor_fileprocessor", app.Repository) ||
                    AreEquivalent("mdsol/Medidata.MOLE", app.Repository) ||
                    AreEquivalent("mdsol/mdsol/Medidata.SLAP", app.Repository) ||
                    AreEquivalent("mdsol/Medidata.Integration", app.Repository) ||
                    AreEquivalent("mdsol/meds_extractor", app.Repository) ||
                    AreEquivalent("mdsol/rave-safety-gateway", app.Repository) ||
                    AreEquivalent("mdsol/Gambit", app.Repository) ||
                    AreEquivalent("mdsol/ShadowBroker", app.Repository) ||
                    AreEquivalent("mdsol/support-portal-api", app.Repository) ||
                    AreEquivalent("mdsol/Medidata.Ampridatvir", app.Repository) ||
                    AreEquivalent("mdsol/neo_rave_etl", app.Repository) ||
                    AreEquivalent("mdsol/kindling", app.Repository) ||
                    AreEquivalent("mdsol/ogrillon", app.Repository) ||
                    AreEquivalent("mdsol/eureka-dotnet-client", app.Repository) ||
                    AreEquivalent("mdsol/batch-upload", app.Repository) ||
                    AreEquivalent("mdsol/SLoginator", app.Repository) ||
                    AreEquivalent("mdsol/platform-logging-dotnet", app.Repository) ||
                    AreEquivalent("mdsol/DictionaryParser", app.Repository) ||
                    AreEquivalent("mdsol/Medidata.Cloud.Shared.Unity", app.Repository) ||
                    AreEquivalent("mdsol/PDFGeneratorUtility", app.Repository) ||
                    AreEquivalent("mdsol/caDSR", app.Repository) ||
                    AreEquivalent("mdsol/medidata-logging-dot-net", app.Repository) ||
                    AreEquivalent("mdsol/belker-dotnet", app.Repository) ||
                    AreEquivalent("mdsol/mauth-client-dotnet", app.Repository) ||
                    AreEquivalent("mdsol/hurl-dotnet", app.Repository) ||
                    AreEquivalent("mdsol/Medidata.MDLogging", app.Repository) ||
                    AreEquivalent("mdsol/medidata-specflow", app.Repository) ||
                    AreEquivalent("mdsol/code_analysis_integration", app.Repository) ||
                    AreEquivalent("mdsol/iMedidata-Site-Admin", app.Repository) ||
                    AreEquivalent("mdsol/RaveAux", app.Repository) ||
                    AreEquivalent("mdsol/medidata.cake", app.Repository) ||
                    AreEquivalent("mdsol/CAREFUL", app.Repository) ||
                    AreEquivalent("mdsol/imedidata-elearning-admin", app.Repository) ||
                    AreEquivalent("mdsol/cs-support-portal-api", app.Repository) ||
                    AreEquivalent("mdsol/Thermometer.RaveCommon", app.Repository) ||
                    AreEquivalent("mdsol/thermometer", app.Repository) ||
                    AreEquivalent("mdsol/Tsdv_Loader", app.Repository) ||
                    AreEquivalent("mdsol/PowerDesigner-Automation", app.Repository) ||
                    AreEquivalent("mdsol/MedidataSpotfire", app.Repository) ||
                    AreEquivalent("mdsol/iMedidata-User-Admin", app.Repository) ||
                    AreEquivalent("mdsol/mdsol/dicebag-dotnet", app.Repository) ||
                    AreEquivalent("mdsol/dotnet-app_status", app.Repository) ||
                    AreEquivalent("mdsol/archon-client-dotnet", app.Repository) ||
                    AreEquivalent("mdsol/Medidata.IFX", app.Repository) ||
                    AreEquivalent("mdsol/Medidata.Notification.NotificationManager", app.Repository) ||
                    AreEquivalent("mdsol/RGAnalyzers", app.Repository) ||
                    AreEquivalent("mdsol//Medidata.IFX.FlightRecorder", app.Repository) ||
                    AreEquivalent("mdsol/Medidata.Messaging", app.Repository) ||
                    AreEquivalent("mdsol/Medidata.Utility.Caching", app.Repository) ||
                    AreEquivalent("mdsol/Medidata.IFX.MachineManager", app.Repository)
                    ))
                .Where(app =>
                    !repoDatetimeDictionary.Any(x =>
                        x.Key.Equals(app.Repository, StringComparison.OrdinalIgnoreCase)) ||
                    repoDatetimeDictionary.Any(x =>
                        x.Key.Equals(app.Repository, StringComparison.OrdinalIgnoreCase) &&
                        !x.Value.Equals(app.UpdatedAt.DateTime)))
                .ToList();
        }

        private bool AreEquivalent(string expectedRepository, string actualRepository) =>
            expectedRepository.Equals(actualRepository, StringComparison.OrdinalIgnoreCase);

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
