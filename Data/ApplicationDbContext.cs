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
        public DbSet<Location> Locations { get; set; } = default!;
        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<CalendarEntry> CalendarEntries { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Explicit key (string PK)
            builder.Entity<EventItem>()
                .HasKey(e => e.EventId);

            // Event -> Location relationship (one-to-many)
            builder.Entity<EventItem>()
                .HasOne(e => e.Location)
                .WithMany(l => l.Events)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Event -> Category relationship (one-to-many)
            builder.Entity<EventItem>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal properties for Location
            builder.Entity<Location>()
                .Property(l => l.Latitude)
                .HasPrecision(10, 8);

            builder.Entity<Location>()
                .Property(l => l.Longitude)
                .HasPrecision(10, 8);

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

            // CalendarEntry -> Event relationship
            builder.Entity<CalendarEntry>()
                .HasOne(ce => ce.Event)
                .WithMany()
                .HasForeignKey(ce => ce.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicates: same user can't sync same event to same provider twice
            builder.Entity<CalendarEntry>()
                .HasIndex(ce => new { ce.UserId, ce.EventId, ce.Provider })
                .IsUnique();
        }
    }
}