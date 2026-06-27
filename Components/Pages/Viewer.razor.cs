using EventTrackerApp.Data;
using EventTrackerApp.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace EventTrackerApp.Components.Pages;

public partial class Viewer
{
    [Inject]
    private AppDbContext DbContext { get; set; } = default!;

    // Calendar State
    private DateTime currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private Dictionary<DateOnly, List<CalendarInstance>>? instancesByDate;

    protected override async Task OnInitializedAsync()
    {
        await GroupInstancesForMonthAsync();
    }

    private async Task GroupInstancesForMonthAsync()
    {
        instancesByDate = null;
        var events = DbContext.Events
            .Include(e => e.Values)
            .ThenInclude(ev => ev.Instances);

        var filteredEvents =
            from evt in events
            from val in evt.Values
            from inst in val.Instances
            where inst.Timestamp.Year == currentMonth.Year
               && inst.Timestamp.Month == currentMonth.Month
            select evt;

        var eventsList =
            await filteredEvents
            .AsNoTracking()
            .Select(e => e.ToViewModel())
            .ToListAsync();

        var query =
            from evt in eventsList
            from val in evt.Values
            from inst in val.Instances
            orderby inst.Timestamp
            group new CalendarInstance(
                inst.Timestamp,
                inst.Details,
                evt.Name,
                evt.Image,
                val.Style)
            by DateOnly.FromDateTime(inst.Timestamp.Date) into g
            select g;

        instancesByDate = new();
        foreach (var group in query)
        {
            instancesByDate[group.Key] = group.ToList();
        }
    }

    private async Task PreviousMonth()
    {
        currentMonth = currentMonth.AddMonths(-1);
        await GroupInstancesForMonthAsync();
    }

    private async Task NextMonth()
    {
        currentMonth = currentMonth.AddMonths(1);
        await GroupInstancesForMonthAsync();
    }

    private async Task GoToToday()
    {
        var wantedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        if (currentMonth != wantedMonth)
        {
            currentMonth = wantedMonth;
            await GroupInstancesForMonthAsync();
        }
    }

    // Lightweight record to pass flattened data to the Razor view
    public record CalendarInstance(
        DateTime Timestamp,
        string Details,
        string EventName,
        string Icon,
        string Style
    );
}