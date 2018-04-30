using System.Collections.Generic;
using System.Linq;
using Medidata.Pikapika.DatabaseAccess;
using Medidata.Pikapika.Miner.Models;

namespace Medidata.Pikapika.Miner.Extensions
{
    public static class DotnetAppExtenstions
    {
        public static IEnumerable<DotnetApps> ConvertToDotnetAppProjectFile(this DotnetApp dotnetApp)
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
    }
}
