using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public DotnetAppsMiner(string authorizationUsername,
            string authorizationToken, string githubBaseUri)
        {
            _githubAccess = new GithubAccess(new Uri(githubBaseUri), authorizationUsername, authorizationToken);
            _githubOfficialClient = new GitHubClient(new ProductHeaderValue(Helpers.Constants.UserAgent))
            {
                Credentials = new Credentials(authorizationToken)
            };
        }

        public async Task<IEnumerable<DotnetApp>> Mine()
        {
            // get all c# mdsol repos
            var dotnetApps = (await GetAllDotnetApps())
                .Where(app => app.Repository.Equals("mdsol/Rave") ||
                    app.Repository.Equals("mdsol/Gambit") ||
                    app.Repository.Equals("mdsol/Medidata.SLAP"));
            Console.WriteLine($"Dotnet apps count: {dotnetApps.Count()}");

            // loop mdsol repos
            foreach (var dotnetApp in dotnetApps)
            {
                var projectFiles = new List<DotnetAppProjectFile>();

                // get projects
                var githubSearchItems = await GetAllProjects(".csproj", "csproj", dotnetApp.Repository);

                // loop through the projects
                foreach (var githubSearchItem in githubSearchItems)
                {
                    var projectFile = githubSearchItem.ConvertToDotnetAppProjectFile();
                    var projectXmlDocument = (await _githubAccess.GetFileContent(projectFile.ProjectContentsUrl))
                        .TryConvertToXDocument(out bool isProjectFileContentValid);

                    if (!isProjectFileContentValid)
                    {
                        Console.WriteLine($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} is very old or invalid!");
                        projectFile.DotnetAppProject = new DotnetAppProject { Frameworks = new string[] { "OLD!" } };
                    }
                    else
                    {
                        if (projectXmlDocument.IsNewCsProjFormat())
                        {
                            Console.WriteLine($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} is new!");
                            projectFile.DotnetAppProject = projectXmlDocument.ConvertToDotnetAppProject();
                        }
                        else
                        {
                            Console.WriteLine($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} is old!");
                            projectFile.DotnetAppProject = new DotnetAppProject
                            {
                                Frameworks = new string[] { projectXmlDocument.GetFrameworkFromOldCsProj() },
                                ProjectNugets = await GetOldCsProjProjectNugets(dotnetApp, projectFile)
                            };
                        }
                    }

                    projectFiles.Add(projectFile);
                }

                dotnetApp.Projects = projectFiles;
                Console.WriteLine(dotnetApp);
            }

            return dotnetApps;
        }

        private async Task<IEnumerable<DotnetApp>> GetAllDotnetApps()
        {
            return (await _githubOfficialClient.Repository.GetAllForOrg("mdsol"))
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
                })
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

        private async Task<IEnumerable<DotnetAppProjectNuget>> GetOldCsProjProjectNugets(DotnetApp dotnetApp, DotnetAppProjectFile projectFile)
        {
            var packagesConfigFile = await SearchFile(
                            "packages.config",
                            "config",
                            dotnetApp.Repository,
                            projectFile.ProjectFilePath.Replace($"/{projectFile.ProjectFileName}", string.Empty));

            if (packagesConfigFile == null)
            {
                Console.WriteLine($"{dotnetApp.Repository}/{projectFile.ProjectFilePath} has no packages.config file!");
                return Enumerable.Empty<DotnetAppProjectNuget>();
            }

            var packagesConfigContent = await _githubAccess.GetFileContent(packagesConfigFile.ContentsUrl);
            var packagesConfigXmlDocument = packagesConfigContent.TryConvertToXDocument(out bool isPackagesConfigFileContentValid);
            if (!isPackagesConfigFileContentValid)
            {
                Console.WriteLine($"{dotnetApp.Repository}/{packagesConfigFile.Path} is not valid!");
                return Enumerable.Empty<DotnetAppProjectNuget>();
            }

            return packagesConfigXmlDocument.GetPackagesConfigReferences();
        }
    }
}
