using Community_Event_Finder.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Community_Event_Finder.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<EventItem> Events { get; set; } = default!;
        public DbSet<Favorite> Favorites { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Explicit key (string PK)
            builder.Entity<EventItem>()
                .HasKey(e => e.EventId);

            // Favorite -> Event relationship
            builder.Entity<Favorite>()
                .HasOne(f => f.Event)
                .WithMany()
                .HasForeignKey(f => f.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicates: same user can't favorite same event twice
            builder.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.EventId })
                .IsUnique();
        }
    }
}