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

                            if (!projectFile.DotnetAppProject.ProjectNugets.Any())
                            {
                                _logger.LogInformation($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} has no nugets! skipping..");
                                continue;
                            }

                            foreach(var nuget in projectFile.DotnetAppProject.ProjectNugets)
                                _logger.LogDebug($"    Nuget: {nuget.Name} {nuget.Version}");
                        }
                        else
                        {
                            _logger.LogInformation($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} is old!");
                            projectFile.DotnetAppProject = new DotnetAppProject
                            {
                                Frameworks = new string[] { projectXmlDocument.GetFrameworkFromOldCsProj() },
                                ProjectNugets = await GetOldCsProjProjectNugets(dotnetApp, projectFile, projectFileContent)
                            };

                            if (!projectFile.DotnetAppProject.ProjectNugets.Any())
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
                    (app.Repository.Equals("mdsol/rave") ||
                    app.Repository.Equals("mdsol/coder") ||
                    app.Repository.Equals("mdsol/Balance-Almac-Drug-Shipping") ||
                    app.Repository.Equals("mdsol/meds_extractor_jobmanager") ||
                    app.Repository.Equals("mdsol/meds_extractor_fileprocessor") ||
                    app.Repository.Equals("mdsol/Medidata.MOLE") ||
                    app.Repository.Equals("mdsol/Medidata.SLAP") ||
                    app.Repository.Equals("mdsol/Medidata.Integration") ||
                    app.Repository.Equals("mdsol/meds_extractor") ||
                    app.Repository.Equals("mdsol/rave-safety-gateway") ||
                    app.Repository.Equals("mdsol/Gambit") ||
                    app.Repository.Equals("mdsol/ShadowBroker") ||
                    app.Repository.Equals("mdsol/support-portal-api") ||
                    app.Repository.Equals("mdsol/Medidata.Ampridatvir") ||
                    app.Repository.Equals("mdsol/neo_rave_etl") ||
                    app.Repository.Equals("mdsol/kindling") ||
                    app.Repository.Equals("mdsol/ogrillon") ||
                    app.Repository.Equals("mdsol/eureka-dotnet-client") ||
                    app.Repository.Equals("mdsol/batch-upload") ||
                    app.Repository.Equals("mdsol/SLoginator") ||
                    app.Repository.Equals("mdsol/platform-logging-dotnet") ||
                    app.Repository.Equals("mdsol/DictionaryParser") ||
                    app.Repository.Equals("mdsol/Medidata.Cloud.Shared.Unity") ||
                    app.Repository.Equals("mdsol/PDFGeneratorUtility") ||
                    app.Repository.Equals("mdsol/caDSR") ||
                    app.Repository.Equals("mdsol/medidata-logging-dot-net") ||
                    app.Repository.Equals("mdsol/belker-dotnet") ||
                    app.Repository.Equals("mdsol/mauth-client-dotnet") ||
                    app.Repository.Equals("mdsol/hurl-dotnet") ||
                    app.Repository.Equals("mdsol/Medidata.MDLogging") ||
                    app.Repository.Equals("mdsol/medidata-specflow") ||
                    app.Repository.Equals("mdsol/code_analysis_integration") ||
                    app.Repository.Equals("mdsol/iMedidata-Site-Admin") ||
                    app.Repository.Equals("mdsol/RaveAux") ||
                    app.Repository.Equals("mdsol/medidata.cake") ||
                    app.Repository.Equals("mdsol/CAREFUL") ||
                    app.Repository.Equals("mdsol/imedidata-elearning-admin") ||
                    app.Repository.Equals("mdsol/cs-support-portal-api") ||
                    app.Repository.Equals("mdsol/Thermometer.RaveCommon") ||
                    app.Repository.Equals("mdsol/thermometer") ||
                    app.Repository.Equals("mdsol/Tsdv_Loader") ||
                    app.Repository.Equals("mdsol/PowerDesigner-Automation") ||
                    app.Repository.Equals("mdsol/MedidataSpotfire") ||
                    app.Repository.Equals("mdsol/iMedidata-User-Admin") ||
                    app.Repository.Equals("mdsol/dicebag-dotnet") ||
                    app.Repository.Equals("mdsol/dotnet-app_status") ||
                    app.Repository.Equals("mdsol/archon-client-dotnet") ||
                    app.Repository.Equals("mdsol/Medidata.IFX") ||
                    app.Repository.Equals("mdsol/Medidata.Notification.NotificationManager") ||
                    app.Repository.Equals("mdsol/RGAnalyzers") ||
                    app.Repository.Equals("mdsol/Medidata.IFX.FlightRecorder") ||
                    app.Repository.Equals("mdsol/Medidata.Messaging") ||
                    app.Repository.Equals("mdsol/Medidata.Utility.Caching") ||
                    app.Repository.Equals("mdsol/Medidata.IFX.MachineManager")))
                .Where(app =>
                    !repoDatetimeDictionary.Any(x =>
                        x.Key.Equals(app.Repository, StringComparison.OrdinalIgnoreCase)) ||
                    repoDatetimeDictionary.Any(x =>
                        x.Key.Equals(app.Repository, StringComparison.OrdinalIgnoreCase) &&
                        !x.Value.Equals(app.UpdatedAt.DateTime)))
                .ToList();
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
