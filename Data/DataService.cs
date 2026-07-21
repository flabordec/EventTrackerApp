using System.Diagnostics.CodeAnalysis;
using EventTrackerApp.Data.Mappers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventTrackerApp.Data;

public interface IDataService
{
    Task AddEventAsync(EventDto newEvent);
    Task AddInstanceAsync(EventInstanceDto instance);
    Task<List<EventDto>> GetEventsAsync(string? userId);
    Task<List<CalendarEventInstanceDto>> GetInstancesForMonthAsync(
        string? userId,
        int year,
        int month,
        TimeZoneInfo? localTimeZone);
}

internal class DefaultDataService : IDataService
{
    private readonly AppDbContext DbContext;

    [Inject]
    [NotNull]
    private UserManager<ApplicationUser>? UserManager { get; }
    [Inject]
    [NotNull]
    private IUserStore<ApplicationUser>? UserStore { get; }
    [Inject]
    [NotNull]
    private SignInManager<ApplicationUser>? SignInManager { get; }

    [Inject]
    [NotNull]
    private Logger<DefaultDataService>? Logger { get; }

    public DefaultDataService(AppDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task AddEventAsync(EventDto eventDto)
    {
        DbContext.Events.Add(eventDto.ToDatabaseEvent());
        await DbContext.SaveChangesAsync();
    }

    public async Task AddInstanceAsync(EventInstanceDto instanceDto)
    {
        DbContext.EventInstances.Add(instanceDto.ToDatabaseInstance());
        await DbContext.SaveChangesAsync();

    }

    public async Task<List<EventDto>> GetEventsAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return [];
        }

        return await DbContext.Events
            .Include(e => e.Values)
            .Where(e => e.UserId == userId)
            .AsNoTracking()
            .Select(e => e.ToEventDto())
            .ToListAsync();
    }

    private DateTimeOffset GetDateTimeOffset(DateTime localTime, TimeZoneInfo? localTimeZone)
    {
        TimeSpan offset = localTimeZone?.GetUtcOffset(localTime) ?? TimeSpan.Zero;
        return new DateTimeOffset(localTime, offset);
    }

    public async Task<List<CalendarEventInstanceDto>> GetInstancesForMonthAsync(
        string? userId,
        int year,
        int month,
        TimeZoneInfo? localTimeZone)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new();
        }

        var startOfMonthLocal = GetDateTimeOffset(new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified), localTimeZone);
        var startOfNextMonthLocal = startOfMonthLocal.AddMonths(1);

        var startOfMonthUtc = startOfMonthLocal.ToUniversalTime();
        var endOfMonthUtc = startOfNextMonthLocal.ToUniversalTime();

        var instancesList = await DbContext.EventInstances
            .Include(inst => inst.EventValue)
            .ThenInclude(val => val!.Event)
            .AsSplitQuery()
            .Where(inst => inst.EventValue!.Event!.UserId == userId
                    && inst.Timestamp >= startOfMonthUtc
                    && inst.Timestamp < endOfMonthUtc)
            .ToListAsync();

        var query =
            from inst in instancesList
            let val = inst.EventValue!
            let evt = val.Event!
            let localTimestamp = inst.Timestamp
            orderby localTimestamp
            select new CalendarEventInstanceDto(
                localTimestamp,
                evt.Name,
                val.Name,
                evt.Image,
                val.ForegroundColor,
                val.BackgroundColor);

        return query.ToList();
    }
}