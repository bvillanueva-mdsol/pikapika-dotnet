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

        public PikapikaRepositoryAccess(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PikapikaContext>();
            optionsBuilder.UseNpgsql(connectionString);
            _options = optionsBuilder.Options;
        }

        public async Task<IEnumerable<DotnetApps>> SaveDotnetApps(IEnumerable<DotnetApps> newDotnetApps)
        {
            var storedDotnetApps = (await GetDotnetApps()).ToList();
            var tobeDeletedApps = storedDotnetApps
                .Where(x => !newDotnetApps
                    .Any(y =>
                        y.Repo.Equals(x.Repo, StringComparison.OrdinalIgnoreCase) &&
                        y.Path.Equals(x.Path, StringComparison.OrdinalIgnoreCase)));
            foreach (var tobeDeletedApp in tobeDeletedApps)
            {
                await DeleteDotnetApp(tobeDeletedApp);
            }
            foreach (var newDotnetApp in newDotnetApps)
            {
                try
                {
                    SaveDotnetApp(newDotnetApp, storedDotnetApps);
                    Console.WriteLine($"Saved App Name:{newDotnetApp.Name}, path: {newDotnetApp.Path} in DB.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in saving App Name:{newDotnetApp.Name}, path: {newDotnetApp.Path},  message: {ex.Message}");
                }
            }

            return await GetDotnetApps();
        }

        public async Task<IEnumerable<DotnetNugets>> SaveDotnetNugets(IEnumerable<DotnetNugets> newDotnetNugets)
        {
            var storedDotnetNugets = (await GetDotnetNugets()).ToList();

            foreach (var newDotnetNuget in newDotnetNugets)
            {
                try
                {
                    SaveDotnetNuget(newDotnetNuget, storedDotnetNugets);
                    Console.WriteLine($"Saved Nuget Name:{newDotnetNuget.Name}, path: {newDotnetNuget.Slug} in DB.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in saving Nuget Name:{newDotnetNuget.Name}, slug: {newDotnetNuget.Slug},  message: {ex.Message}");
                }
            }

            return await GetDotnetNugets();
        }

        public async Task SaveDotnetAppDotnetNugetRelationships(IEnumerable<DotnetAppDotnetNugets> dotnetAppDotnetNugets)
        {
            var storedDotnetAppDotnetNugets = (await GetDotnetAppsDotnetNugets()).ToList();
            foreach (var dotnetAppDotnetNuget in dotnetAppDotnetNugets)
            {
                try
                {
                    SaveDotnetAppDotnetNugetRelationship(dotnetAppDotnetNuget, storedDotnetAppDotnetNugets);
                    Console.WriteLine($"Saved DotnetAppDotnetNugetRelationship AppId:{dotnetAppDotnetNuget.DotnetAppId}, NugetId: {dotnetAppDotnetNuget.DotnetNugetId} in DB.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in saving DotnetAppDotnetNugetRelationship AppId:{dotnetAppDotnetNuget.DotnetAppId}, NugetId: {dotnetAppDotnetNuget.DotnetNugetId},  message: {ex.Message}");
                }
            }
        }

        private void SaveDotnetApp(DotnetApps dotnetApp, List<DotnetApps> storedDotnetApps)
        {
            var storedDotnetApp = storedDotnetApps
                .Where(x =>
                        x.Repo.Equals(dotnetApp.Repo, StringComparison.OrdinalIgnoreCase) &&
                        x.Path.Equals(dotnetApp.Path, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            using (var context = new PikapikaContext(_options))
            {
                if (storedDotnetApp != null)
                {
                    dotnetApp.Id = storedDotnetApp.Id;
                    context.DotnetApps.Update(dotnetApp);
                }
                else
                {
                    context.DotnetApps.Add(dotnetApp);
                }
                context.SaveChanges();
            }  
        }

        private void SaveDotnetNuget(DotnetNugets dotnetNuget, List<DotnetNugets> storedDotnetNugets)
        {
            var storedDotnetNuget = storedDotnetNugets
                .Where(x =>
                        x.Slug.Equals(dotnetNuget.Slug, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            using (var context = new PikapikaContext(_options))
            {
                if (storedDotnetNuget != null)
                {
                    dotnetNuget.Id = storedDotnetNuget.Id;
                    context.DotnetNugets.Update(dotnetNuget);
                }
                else
                {
                    context.DotnetNugets.Add(dotnetNuget);
                }
                context.SaveChanges();
            } 
        }

        private void SaveDotnetAppDotnetNugetRelationship(DotnetAppDotnetNugets dotnetAppDotnetNuget, List<DotnetAppDotnetNugets> storedDotnetAppDotnetNugets)
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
                    dotnetAppDotnetNuget.Id = storedDotnetAppDotnetNuget.Id;
                    context.DotnetAppDotnetNugets.Update(dotnetAppDotnetNuget);
                }
                else
                {
                    context.DotnetAppDotnetNugets.Add(dotnetAppDotnetNuget);
                }
                context.SaveChanges();
            }
        }

        private async Task<IEnumerable<DotnetApps>> GetDotnetApps()
        {
            using (var context = new PikapikaContext(_options))
            {
                return await context.DotnetApps.ToListAsync();
            }
        }

        private async Task<IEnumerable<DotnetNugets>> GetDotnetNugets()
        {
            using (var context = new PikapikaContext(_options))
            {
                return await context.DotnetNugets.ToListAsync();
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
    }
}
