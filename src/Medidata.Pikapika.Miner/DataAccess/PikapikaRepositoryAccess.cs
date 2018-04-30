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

        public async Task PushData(IEnumerable<DotnetApps> newDotnetApps)
        {
            var storedDotnetApps = await GetDotnetApps();
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
                    Save(newDotnetApp, storedDotnetApps.ToList());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in saving Name:{newDotnetApp.Name}, path: {newDotnetApp.Path},  message: {ex.Message}");
                }
            }
        }

        private void Save(DotnetApps dotnetApp, List<DotnetApps> storedDotnetApps)
        {
            var storedDotnetApp = storedDotnetApps
                .Where(x =>
                        x.Repo.Equals(dotnetApp.Repo, StringComparison.OrdinalIgnoreCase) &&
                        x.Path.Equals(dotnetApp.Path, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (storedDotnetApp != null)
            {
                dotnetApp.Id = storedDotnetApp.Id;
                using (var context = new PikapikaContext(_options))
                {
                    context.DotnetApps.Update(dotnetApp);
                    context.SaveChanges();
                }
            }
            else
            {
                using (var context = new PikapikaContext(_options))
                {
                    context.DotnetApps.Add(dotnetApp);
                    context.SaveChanges();
                }
            }
        }

        private async Task<IEnumerable<DotnetApps>> GetDotnetApps()
        {
            using (var context = new PikapikaContext(_options))
            {
                return await context.DotnetApps.ToListAsync();
            }
        }

        private async Task<DotnetApps> GetDotnetApp(DotnetApps dotnetApp)
        {
            using (var context = new PikapikaContext(_options))
            {
                return await context.DotnetApps
                    .Where(x => 
                        x.Repo.Equals(dotnetApp.Repo, StringComparison.OrdinalIgnoreCase) &&
                        x.Path.Equals(dotnetApp.Path, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefaultAsync();
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
