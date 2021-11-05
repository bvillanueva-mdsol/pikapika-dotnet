using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess
{
    public partial class PikapikaContext : DbContext
    {
        public virtual DbSet<DotnetAppDotnetNugets> DotnetAppDotnetNugets { get; set; }
        public virtual DbSet<DotnetApps> DotnetApps { get; set; }
        public virtual DbSet<DotnetNugets> DotnetNugets { get; set; }

        public PikapikaContext(DbContextOptions<PikapikaContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("citext")
                .HasPostgresExtension("pg_trgm");

            modelBuilder.Entity<DotnetAppDotnetNugets>(entity =>
            {
                entity.ToTable("dotnet_app_dotnet_nugets");

                entity.HasIndex(e => e.DotnetAppId)
                    .HasDatabaseName("index_dotnet_app_dotnet_nugets_on_dotnet_app_id");

                entity.HasIndex(e => e.DotnetNugetId)
                    .HasDatabaseName("index_dotnet_app_dotnet_nugets_on_dotnet_nuget_id");

                entity.HasIndex(e => new { e.DotnetAppId, e.DotnetNugetId })
                    .HasDatabaseName("dotnet_app_nuget")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.DotnetAppId).HasColumnName("dotnet_app_id");

                entity.Property(e => e.DotnetNugetId).HasColumnName("dotnet_nuget_id");

                entity.Property(e => e.Ref)
                    .HasColumnName("ref")
                    .HasColumnType("citext");

                entity.Property(e => e.Version)
                    .HasColumnName("version")
                    .HasColumnType("citext");

                entity.HasOne(d => d.DotnetApp)
                    .WithMany(p => p.DotnetAppDotnetNugets)
                    .HasForeignKey(d => d.DotnetAppId)
                    .HasConstraintName("fk_rails_e92c45c77e");

                entity.HasOne(d => d.DotnetNuget)
                    .WithMany(p => p.DotnetAppDotnetNugets)
                    .HasForeignKey(d => d.DotnetNugetId)
                    .HasConstraintName("fk_rails_e36c13f188");
            });

            modelBuilder.Entity<DotnetApps>(entity =>
            {
                entity.ToTable("dotnet_apps");

                entity.HasIndex(e => e.Deployed)
                    .HasDatabaseName("index_dotnet_apps_on_deployed");

                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("index_dotnet_apps_on_name")
                    .IsUnique();

                entity.HasIndex(e => new { e.Repo, e.Path })
                    .HasDatabaseName("index_dotnet_apps_on_repo_and_path")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.Property(e => e.DefaultBranch)
                    .HasColumnName("default_branch")
                    .HasColumnType("citext");

                entity.Property(e => e.Deployed)
                    .HasColumnName("deployed")
                    .HasDefaultValueSql("true");

                entity.Property(e => e.Deprecated)
                    .HasColumnName("deprecated")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("citext");

                entity.Property(e => e.Path)
                    .HasColumnName("path")
                    .HasColumnType("citext");

                entity.Property(e => e.Primary)
                    .HasColumnName("primary")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.Repo)
                    .HasColumnName("repo")
                    .HasColumnType("citext");

                entity.Property(e => e.SandboxDomain)
                    .HasColumnName("sandbox_domain")
                    .HasColumnType("citext");

                entity.Property(e => e.Slug)
                    .HasColumnName("slug")
                    .HasColumnType("citext");

                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.Property(e => e.Url)
                    .HasColumnName("url")
                    .HasColumnType("citext");
            });

            modelBuilder.Entity<DotnetNugets>(entity =>
            {
                entity.ToTable("dotnet_nugets");

                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("index_dotnet_nugets_on_name");

                entity.HasIndex(e => e.Slug)
                    .HasDatabaseName("index_dotnet_nugets_on_slug")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.Property(e => e.DefaultBranch)
                    .HasColumnName("default_branch")
                    .HasColumnType("citext");

                entity.Property(e => e.Deprecated)
                    .HasColumnName("deprecated")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.Gemspec)
                    .HasColumnName("gemspec")
                    .HasColumnType("citext");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("citext");

                entity.Property(e => e.Oss)
                    .HasColumnName("oss")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.Published)
                    .HasColumnName("published")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.Repo)
                    .HasColumnName("repo")
                    .HasColumnType("citext");

                entity.Property(e => e.Slug)
                    .HasColumnName("slug")
                    .HasColumnType("citext");

                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.Property(e => e.Url)
                    .HasColumnName("url")
                    .HasColumnType("citext");

                entity.Property(e => e.Versions)
                    .HasColumnName("versions")
                    .HasColumnType("jsonb");
            });
        }
    }
}
