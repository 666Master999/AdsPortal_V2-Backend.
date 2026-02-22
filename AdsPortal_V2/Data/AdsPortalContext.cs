// Data/AdsPortalContext.cs
using AdsPortal_V2.Models;
using Microsoft.EntityFrameworkCore;

namespace AdsPortal_V2.Data
{
    public class AdsPortalContext(DbContextOptions<AdsPortalContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<AdsPortal_V2.Models.Ad> Ads { get; set; } = null!;
        public DbSet<AdImage> AdImages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Login)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Ad>()
                .HasOne(a => a.Owner)
                .WithMany(u => u.Ads)
                .HasForeignKey(a => a.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdImage>()
                .HasOne(i => i.Ad)
                .WithMany(a => a.Images)
                .HasForeignKey(i => i.AdId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdImage>()
                .HasIndex(i => i.AdId);
        }
    }
}