using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medidata.Pikapika.DatabaseAccess;
using Microsoft.EntityFrameworkCore;

namespace Medidata.Pikapika.Miner.DataAccess
{
    public class PikapikaRepositoryAccess
    {
        protected readonly DbContextOptions<PikapikaContext> _options;

        private Logger _logger;

        public PikapikaRepositoryAccess(string connectionString, Logger logger)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PikapikaContext>();
            optionsBuilder.UseNpgsql(connectionString);
            _options = optionsBuilder.Options;
            _logger = logger;
        }

        public async Task<IEnumerable<DotnetApps>> SaveDotnetApps(IEnumerable<DotnetApps> newDotnetApps)
        {
            await DeleteNonExistingApps(newDotnetApps);
            var savedApps = new List<DotnetApps>();

            foreach (var newDotnetApp in newDotnetApps)
            {
                try
                {
                    var savedApp = await SaveDotnetApp(newDotnetApp);
                    savedApps.Add(savedApp);
                    _logger.LogInformation($"Saved App Name: {savedApp.Name}, path: {savedApp.Repo}/{savedApp.Path} in DB.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in saving App Name:{newDotnetApp.Name}, path: {newDotnetApp.Repo}/{newDotnetApp.Path}, message: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Error in saving App Name:{newDotnetApp.Name}, path: {newDotnetApp.Repo}/{newDotnetApp.Path}, message: {ex.InnerException.Message}");
                    }
                }
            }

            return await GetDotnetApps(savedApps);
        }

        public async Task<IEnumerable<DotnetNugets>> SaveDotnetNugets(IEnumerable<DotnetNugets> newDotnetNugets)
        {
            var dotnetNugetsFromDb = await GetDotnetNugets();

            foreach (var newDotnetNuget in newDotnetNugets)
            {
                try
                {
                    SaveDotnetNuget(newDotnetNuget, dotnetNugetsFromDb);
                    _logger.LogInformation($"Saved Nuget Name:{newDotnetNuget.Name}, path: {newDotnetNuget.Slug} in DB.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in saving Nuget Name:{newDotnetNuget.Name}, slug: {newDotnetNuget.Slug}, message: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Error in saving Nuget Name:{newDotnetNuget.Name}, slug: {newDotnetNuget.Slug}, message: {ex.InnerException.Message}");
                    }
                }
            }

            return await GetDotnetNugets();
        }

        public async Task SaveDotnetAppDotnetNugetRelationships(IEnumerable<DotnetAppDotnetNugets> dotnetAppDotnetNugets)
        {
            var dotnetAppNugetsRelationshipFromDb = await GetDotnetAppsDotnetNugets();
            var tobeDeletedAppNugetsRelationship = dotnetAppNugetsRelationshipFromDb
                    .Where(x =>
                        dotnetAppDotnetNugets.Any(y =>
                            y.DotnetAppId.Value == x.DotnetAppId.Value) &&
                        !dotnetAppDotnetNugets.Any(y =>
                            y.DotnetAppId.Value == x.DotnetAppId.Value &&
                            y.DotnetNugetId.Value == x.DotnetNugetId.Value));
            foreach (var tobeDeletedAppNugetRelationship in tobeDeletedAppNugetsRelationship)
            {
                _logger.LogWarning($"Deleting in App Nuget Relationship AppId:{tobeDeletedAppNugetRelationship.DotnetAppId}, NugetId: {tobeDeletedAppNugetRelationship.DotnetNugetId}");
                DeleteDotnetAppNugetRelationship(tobeDeletedAppNugetRelationship);
            }

            foreach (var dotnetAppDotnetNuget in dotnetAppDotnetNugets)
            {
                try
                {
                    SaveDotnetAppDotnetNugetRelationship(dotnetAppDotnetNuget, dotnetAppNugetsRelationshipFromDb);
                    _logger.LogInformation($"Saved DotnetAppDotnetNugetRelationship AppId:{dotnetAppDotnetNuget.DotnetAppId}, NugetId: {dotnetAppDotnetNuget.DotnetNugetId} in DB.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in saving DotnetAppDotnetNugetRelationship AppId:{dotnetAppDotnetNuget.DotnetAppId}, NugetId: {dotnetAppDotnetNuget.DotnetNugetId}, message: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner Exception in saving DotnetAppDotnetNugetRelationship AppId:{dotnetAppDotnetNuget.DotnetAppId}, NugetId: {dotnetAppDotnetNuget.DotnetNugetId}, message: {ex.InnerException.Message}");
                    }
                }
            }
        }

        public async Task<IEnumerable<DotnetApps>> GetDotnetApps()
        {
            using (var context = new PikapikaContext(_options))
            {
                return await context.DotnetApps.ToListAsync();
            }
        }

        public async Task<bool> HasDuplicateDotnetApp(string name, string repo, string path)
        {
            using (var context = new PikapikaContext(_options))
            {
                var idCombination = $"{repo}{path}";
                return (await context.DotnetApps
                    .Where(x =>
                        name.Equals(x.Name, StringComparison.OrdinalIgnoreCase) &&
                        !idCombination.Equals($"{x.Repo}{x.Path}", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefaultAsync()) != null;
            }
        }

        public async Task<DotnetApps> GetDotnetApp(string repo, string path)
        {
            using (var context = new PikapikaContext(_options))
            {
                return await context.DotnetApps
                    .Where(x =>
                        repo.Equals(x.Repo, StringComparison.OrdinalIgnoreCase) &&
                        path.Equals(x.Path, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefaultAsync();
            }
        }

        public async Task<IEnumerable<DotnetApps>> GetDotnetApps(IEnumerable<DotnetApps> apps)
        {
            using (var context = new PikapikaContext(_options))
            {
                return await context.DotnetApps
                    .Where(x =>
                        apps.Any(y =>
                            y.Repo.Equals(x.Repo, StringComparison.OrdinalIgnoreCase) &&
                            y.Path.Equals(x.Path, StringComparison.OrdinalIgnoreCase)))
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<DotnetNugets>> GetDotnetNugets()
        {
            using (var context = new PikapikaContext(_options))
            {
                return await context.DotnetNugets.ToListAsync();
            }
        }

        private async Task DeleteNonExistingApps(IEnumerable<DotnetApps> newDotnetApps)
        {
            var dotnetAppsFromDb = await GetDotnetApps();
            var tobeDeletedApps = dotnetAppsFromDb
                    .Where(x =>
                        newDotnetApps.Any(y =>
                            y.Repo.Equals(x.Repo, StringComparison.OrdinalIgnoreCase)) &&
                        !newDotnetApps.Any(y =>
                            y.Repo.Equals(x.Repo, StringComparison.OrdinalIgnoreCase) &&
                            y.Path.Equals(x.Path, StringComparison.OrdinalIgnoreCase)));
            foreach (var tobeDeletedApp in tobeDeletedApps)
            {
                _logger.LogWarning($"Deleting in App Name:{tobeDeletedApp.Name}, path: {tobeDeletedApp.Repo}/{tobeDeletedApp.Path}");
                await DeleteDotnetApp(tobeDeletedApp);
            }
        }

        private async Task<DotnetApps> SaveDotnetApp(DotnetApps dotnetApp)
        {
            var existingDotnetApp = await GetDotnetApp(dotnetApp.Repo, dotnetApp.Path);
            if (await HasDuplicateDotnetApp(dotnetApp.Name, dotnetApp.Repo, dotnetApp.Path))
                dotnetApp.Name = ($"{dotnetApp.Repo.Replace("mdsol/", string.Empty)}_{dotnetApp.Name}").Replace(".csproj", string.Empty);

            using (var context = new PikapikaContext(_options))
            {
                if (existingDotnetApp != null)
                {
                    dotnetApp.Id = existingDotnetApp.Id;
                    context.DotnetApps.Update(dotnetApp);
                }
                else
                {
                    context.DotnetApps.Add(dotnetApp);
                }
                context.SaveChanges();
            }

            return dotnetApp;
        }

        private void SaveDotnetNuget(DotnetNugets dotnetNuget, IEnumerable<DotnetNugets> storedDotnetNugets)
        {
            var storedDotnetNuget = storedDotnetNugets
                .Where(x =>
                        x.Slug.Equals(dotnetNuget.Slug, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            using (var context = new PikapikaContext(_options))
            {
                if (storedDotnetNuget != null)
                {
                    if (storedDotnetNuget.UpdatedAt == dotnetNuget.UpdatedAt)
                    {
                        _logger.LogInformation($"Nuget {dotnetNuget.Name} did not change, no db operation for this nuget.");
                        return;
                    }

                    _logger.LogInformation($"Nuget {dotnetNuget.Name} needs updating.");
                    dotnetNuget.Id = storedDotnetNuget.Id;
                    context.DotnetNugets.Update(dotnetNuget);
                }
                else
                {
                    _logger.LogInformation($"Nuget {dotnetNuget.Name} is new.");
                    context.DotnetNugets.Add(dotnetNuget);
                }
                context.SaveChanges();
            }
        }

        private void SaveDotnetAppDotnetNugetRelationship(DotnetAppDotnetNugets dotnetAppDotnetNuget, IEnumerable<DotnetAppDotnetNugets> storedDotnetAppDotnetNugets)
        {
            var storedDotnetAppDotnetNuget = storedDotnetAppDotnetNugets
                .Where(x =>
                        x.DotnetAppId.Value == dotnetAppDotnetNuget.DotnetAppId.Value &&
                        x.DotnetNugetId.Value == dotnetAppDotnetNuget.DotnetNugetId.Value)
                .FirstOrDefault();

            using (var context = new PikapikaContext(_options))
            {
                if (storedDotnetAppDotnetNuget != null)
                {
                    if (storedDotnetAppDotnetNuget.Version == dotnetAppDotnetNuget.Version)
                    {
                        _logger.LogInformation($"DotnetAppDotnetNugetRelationship AppId:{dotnetAppDotnetNuget.DotnetAppId}, NugetId: {dotnetAppDotnetNuget.DotnetNugetId} did not change.");
                        return;
                    }

                    _logger.LogInformation($"DotnetAppDotnetNugetRelationship AppId:{dotnetAppDotnetNuget.DotnetAppId}, NugetId: {dotnetAppDotnetNuget.DotnetNugetId} needs updating.");
                    dotnetAppDotnetNuget.Id = storedDotnetAppDotnetNuget.Id;
                    context.DotnetAppDotnetNugets.Update(dotnetAppDotnetNuget);
                }
                else
                {
                    _logger.LogInformation($"DotnetAppDotnetNugetRelationship AppId:{dotnetAppDotnetNuget.DotnetAppId}, NugetId: {dotnetAppDotnetNuget.DotnetNugetId} is new.");
                    context.DotnetAppDotnetNugets.Add(dotnetAppDotnetNuget);
                }
                context.SaveChanges();
            }
        }

        private async Task<IEnumerable<DotnetAppDotnetNugets>> GetDotnetAppsDotnetNugets()
        {
            using (var context = new PikapikaContext(_options))
            {
                return await context.DotnetAppDotnetNugets.ToListAsync();
            }
        }

        private async Task DeleteDotnetApp(DotnetApps dotnetApp)
        {
            using (var context = new PikapikaContext(_options))
            {
                foreach (var reference in await context.DotnetAppDotnetNugets
                    .Where(reference => reference.DotnetAppId == dotnetApp.Id).ToListAsync())
                {
                    context.Entry(reference).State = EntityState.Deleted;
                }
                context.Entry(dotnetApp).State = EntityState.Deleted;

                context.SaveChanges();
            }
        }

        private void DeleteDotnetAppNugetRelationship(DotnetAppDotnetNugets dotnetAppDotnetNugets)
        {
            using (var context = new PikapikaContext(_options))
            {
                context.Entry(dotnetAppDotnetNugets).State = EntityState.Deleted;

                context.SaveChanges();
            }
        }
    }
}
