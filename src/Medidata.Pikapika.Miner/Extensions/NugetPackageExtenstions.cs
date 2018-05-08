using System;
using System.Collections.Generic;
using System.Linq;
using Medidata.Pikapika.DatabaseAccess;
using Medidata.Pikapika.Miner.Models;
using Newtonsoft.Json;

namespace Medidata.Pikapika.Miner.Extensions
{
    public static class NugetPackageExtenstions
    {
        public static DotnetNugets ConvertToDotnetNugets(this IEnumerable<NugetPackage> packageSearchMetadatas)
        {
            var oldest = packageSearchMetadatas.First();
            var latest = packageSearchMetadatas.Last();
            var id = latest.PackageSearchMetadata.Identity.Id;
            var url = latest.PackageSearchMetadata.ProjectUrl?.AbsoluteUri;
            var createdAt = oldest.PackageSearchMetadata.Published.Value.DateTime;
            var updatedAt = latest.PackageSearchMetadata.Published.Value.DateTime;
            var versions = JsonConvert.SerializeObject(
                packageSearchMetadatas
                    .Reverse() // pikapika ui expects latest as first
                    .Select(x => new
                    {
                        version = x.PackageSearchMetadata.Identity.Version.ToFullString(),
                        timestamp = DateTime.SpecifyKind(x.PackageSearchMetadata.Published.Value.DateTime, DateTimeKind.Utc).ToString("o")
                    })
                    .ToList());
            var oss = latest is OssNugetPackage;

            return new DotnetNugets
            {
                Name = id,
                Slug = id,
                Repo = id,
                Url = url,
                DefaultBranch = "TODO",
                Gemspec = "TODO",
                Versions = versions,
                Deprecated = false,
                Published = true,
                Oss = oss,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };
        }
    }
}
