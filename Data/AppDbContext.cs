using Microsoft.EntityFrameworkCore;

namespace EventTrackerApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventValue> EventValues => Set<EventValue>();
    public DbSet<EventInstance> EventInstances => Set<EventInstance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("event_data");

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<EventValue>(entity =>
        {
            entity.HasKey(ev => ev.Id);
            entity.HasOne(ev => ev.Event)
                  .WithMany(e => e.Values)
                  .HasForeignKey(nameof(EventValue.EventId));
        });

        modelBuilder.Entity<EventInstance>(entity =>
        {
            entity.HasKey(ei => ei.Id);
            entity.HasOne(ei => ei.EventValue)
                  .WithMany(ev => ev.Instances)
                  .HasForeignKey(nameof(EventInstance.EventValueId));
        });
    }
}