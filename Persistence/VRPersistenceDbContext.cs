using Microsoft.EntityFrameworkCore;
using Persistence.DbEntities;

namespace Persistence
{
    public class VRPersistenceDbContext : DbContext
    {
        public DbSet<Release> Releases { get; set; }
        public DbSet<Media> Media { get; set; }
        public DbSet<NotificationEndpoint> NotificationEndpoints { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public VRPersistenceDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Media>(e => e
                .HasIndex(m => m.MediaName)
                .IsUnique()
            );
            modelBuilder.Entity<NotificationEndpoint>(e => e
                .HasIndex(n => n.Identifier)
                .IsUnique()
            );
            var subscriptionModel = modelBuilder.Entity<Subscription>();
            subscriptionModel
                .HasIndex(s => new {MediaId = s.Media.Id, NotificationEndpointId = s.NotificationEndpoint.Id})
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}