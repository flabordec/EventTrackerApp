using Microsoft.EntityFrameworkCore;

namespace EventTrackerApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventInstance> EventInstances => Set<EventInstance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<EventInstance>(entity =>
        {
            entity.HasKey(ei => ei.Id);
            entity.HasOne(ei => ei.Event)
                  .WithMany()
                  .HasForeignKey("EventId");
        });
    }
}