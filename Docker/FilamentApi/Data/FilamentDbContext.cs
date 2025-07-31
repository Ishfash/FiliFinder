using Microsoft.EntityFrameworkCore;
using FilamentApi.Models;

namespace FilamentApi.Data
{
    public class FilamentDbContext : DbContext
    {
        public FilamentDbContext(DbContextOptions<FilamentDbContext> options) : base(options)
        {
        }

        public DbSet<Swatch> Swatches { get; set; } = null!;
        public DbSet<Manufacturer> Manufacturers { get; set; } = null!;
        public DbSet<FilamentType> FilamentTypes { get; set; } = null!;
        public DbSet<PantoneColor> PantoneColors { get; set; } = null!;
        public DbSet<PmsColor> PmsColors { get; set; } = null!;
        public DbSet<RalColor> RalColors { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Swatch entity
            modelBuilder.Entity<Swatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // Use API ID

                entity.HasIndex(e => e.ColorParent);
                entity.HasIndex(e => e.HexColor);
                entity.HasIndex(e => e.ManufacturerId);
                entity.HasIndex(e => e.FilamentTypeId);
                entity.HasIndex(e => e.DatePublished);

                entity.Property(e => e.TdRange)
                    .HasConversion(
                        v => v == null ? null : string.Join(',', v),
                        v => v == null ? null : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).ToArray());

                // Configure relationships
                entity.HasOne(s => s.Manufacturer)
                    .WithMany(m => m.Swatches)
                    .HasForeignKey(s => s.ManufacturerId);

                entity.HasOne(s => s.FilamentType)
                    .WithMany(ft => ft.Swatches)
                    .HasForeignKey(s => s.FilamentTypeId);
            });

            // Configure Manufacturer entity
            modelBuilder.Entity<Manufacturer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // Use API ID
                entity.HasIndex(e => e.Name);
            });

            // Configure FilamentType entity
            modelBuilder.Entity<FilamentType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // Use API ID
                entity.HasIndex(e => e.Name);

                entity.OwnsOne(ft => ft.ParentType);
            });

            // Configure color entities
            modelBuilder.Entity<PantoneColor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(pc => pc.Swatch)
                    .WithMany(s => s.PantoneColors)
                    .HasForeignKey(pc => pc.SwatchId);
            });

            modelBuilder.Entity<PmsColor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(pc => pc.Swatch)
                    .WithMany(s => s.PmsColors)
                    .HasForeignKey(pc => pc.SwatchId);
            });

            modelBuilder.Entity<RalColor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(rc => rc.Swatch)
                    .WithMany(s => s.RalColors)
                    .HasForeignKey(rc => rc.SwatchId);
            });
        }
    }
}