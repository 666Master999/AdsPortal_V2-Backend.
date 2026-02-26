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
            // Explicit User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).ValueGeneratedOnAdd();
                entity.Property(u => u.Login).IsRequired().HasMaxLength(50);
                entity.Property(u => u.UserName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Email).HasMaxLength(256);
                entity.Property(u => u.Phone).HasMaxLength(20);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.PasswordSalt).IsRequired();
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(u => u.Role).HasConversion<int>();
                entity.Property(u => u.IsBlocked).HasDefaultValue(false);
                entity.HasIndex(u => u.Login).IsUnique();
            });

            // Explicit Ad configuration
            modelBuilder.Entity<Ad>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Id).ValueGeneratedOnAdd();
                entity.Property(a => a.Title).IsRequired();
                entity.Property(a => a.Price).HasColumnType("decimal(18,2)");
                entity.Property(a => a.Type).HasConversion<int>();
                entity.Property(a => a.IsNegotiable).HasDefaultValue(false);
                entity.Property(a => a.IsHidden).HasDefaultValue(false);
                entity.Property(a => a.IsDeleted).HasDefaultValue(false);
                entity.HasOne(a => a.Owner)
                      .WithMany(u => u.Ads)
                      .HasForeignKey(a => a.OwnerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Explicit AdImage configuration  
            modelBuilder.Entity<AdImage>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Id).ValueGeneratedOnAdd();
                entity.Property(i => i.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(i => i.IsMain).HasDefaultValue(false);
                entity.Property(i => i.Order).HasDefaultValue(0);
                entity.HasOne(i => i.Ad)
                      .WithMany(a => a.Images)
                      .HasForeignKey(i => i.AdId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(i => i.AdId);
            });
        }
    }
}