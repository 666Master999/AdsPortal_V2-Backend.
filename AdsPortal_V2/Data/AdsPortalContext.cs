// Data/AdsPortalContext.cs
using AdsPortal_V2.Models;
using Microsoft.EntityFrameworkCore;

namespace AdsPortal_V2.Data
{
    public class AdsPortalContext(DbContextOptions<AdsPortalContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Login)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}