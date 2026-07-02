using EventTrackerApp.Data;
using EventTrackerApp.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace EventTrackerApp.Data;

public interface IDataService
{
    Task AddEvent(Event newEvent);
    Task AddInstance(EventInstance instance);
    Task<List<EventViewModel>> GetEvents(string? userId);
    Task<Dictionary<DateOnly, List<CalendarInstance>>> GroupInstancesForMonthAsync(
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

    public async Task AddEvent(Event newEvent)
    {

        DbContext.Events.Add(newEvent);
        await DbContext.SaveChangesAsync();
    }

    public async Task AddInstance(EventInstance instance)
    {
        DbContext.EventInstances.Add(instance);
        await DbContext.SaveChangesAsync();

    }

    public async Task<List<EventViewModel>> GetEvents(string? userId)
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

    public DateTimeOffset GetDateTimeOffset(DateTime localTime, TimeZoneInfo? localTimeZone)
    {
        TimeSpan offset = localTimeZone?.GetUtcOffset(localTime) ?? TimeSpan.Zero;
        return new DateTimeOffset(localTime, offset);
    }

    public async Task<Dictionary<DateOnly, List<CalendarInstance>>> GroupInstancesForMonthAsync(
        string? userId,
        int year,
        int month,
        TimeZoneInfo? localTimeZone)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new Dictionary<DateOnly, List<CalendarInstance>>();
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
            let localTimestamp = ToDateTimeOffset(inst.Timestamp, localTimeZone)
            orderby localTimestamp
            group new CalendarInstance(
                localTimestamp,
                inst.Details,
                evt.Name,
                val.Name,
                evt.Image,
                val.Style)
            by DateOnly.FromDateTime(localTimestamp.Date) into g
            select g;

        var instancesByDate = new Dictionary<DateOnly, List<CalendarInstance>>();
        foreach (var group in query)
        {
            instancesByDate[group.Key] = group.ToList();
        }
        return instancesByDate;
    }

    private static DateTimeOffset ToDateTimeOffset(DateTimeOffset dateTime, TimeZoneInfo? localTimeZone)
    {
        return dateTime.ToOffset(localTimeZone?.GetUtcOffset(dateTime.DateTime) ?? TimeSpan.Zero);
    }
}