using EventTrackerApp.Data;
using EventTrackerApp.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace EventTrackerApp.Data;

public interface IDataService
{
    Task AddEventAsync(Event newEvent);
    Task AddInstanceAsync(EventInstance instance);
    Task<List<EventViewModel>> GetEventsAsync(string? userId);
    Task<List<CalendarInstance>> GetInstancesForMonthAsync(
        string? userId,
        int year,
        int month,
        TimeZoneInfo? localTimeZone);
}

public class DefaultDataService : IDataService
{
    private readonly AppDbContext DbContext;

    public DefaultDataService(AppDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task AddEventAsync(Event newEvent)
    {

        DbContext.Events.Add(newEvent);
        await DbContext.SaveChangesAsync();
    }

    public async Task AddInstanceAsync(EventInstance instance)
    {
        DbContext.EventInstances.Add(instance);
        await DbContext.SaveChangesAsync();

    }

    public async Task<List<EventViewModel>> GetEventsAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return [];
        }

        return await DbContext.Events
            .Include(e => e.Values)
            .Where(e => e.UserId == userId)
            .AsNoTracking()
            .Select(e => e.ToViewModel())
            .ToListAsync();
    }

    private DateTimeOffset GetDateTimeOffset(DateTime localTime, TimeZoneInfo? localTimeZone)
    {
        TimeSpan offset = localTimeZone?.GetUtcOffset(localTime) ?? TimeSpan.Zero;
        return new DateTimeOffset(localTime, offset);
    }

    public async Task<List<CalendarInstance>> GetInstancesForMonthAsync(
        string? userId,
        int year,
        int month,
        TimeZoneInfo? localTimeZone)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new();
        }


        var events = DbContext.Events
            .Include(e => e.Values)
            .ThenInclude(ev => ev.Instances)
            .AsSplitQuery()
            .Where(e => e.UserId == userId);

        var startOfMonthLocal = GetDateTimeOffset(new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified), localTimeZone);
        var startOfNextMonthLocal = startOfMonthLocal.AddMonths(1);

        var startOfMonthUtc = startOfMonthLocal.ToUniversalTime();
        var endOfMonthUtc = startOfNextMonthLocal.ToUniversalTime();

        var eventsList = await (
            from evt in events
            from val in evt.Values
            from inst in val.Instances
            where inst.Timestamp >= startOfMonthUtc && inst.Timestamp < endOfMonthUtc
            select new { evt, val, inst }
        ).ToListAsync();

        var query =
            from evtGroup in eventsList
            let evt = evtGroup.evt.ToViewModel()
            let val = evtGroup.val.ToViewModel()
            let inst = evtGroup.inst.ToViewModel()
            let localTimestamp = inst.Timestamp
            orderby localTimestamp
            select new CalendarInstance(
                localTimestamp,
                inst.Details,
                evt.Name,
                val.Name,
                evt.Image,
                val.Style);

        return query.ToList();
    }
}