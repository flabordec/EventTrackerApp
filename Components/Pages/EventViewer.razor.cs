using System.Diagnostics.CodeAnalysis;
using EventTrackerApp.Data;
using EventTrackerApp.Helpers;
using EventTrackerApp.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EventTrackerApp.Components.Pages;

public partial class EventViewer
{
    [Inject]
    [NotNull]
    private ILogger<EventViewer>? Logger { get; set; }

    // Calendar State
    private DateTime currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private IEnumerable<TimeZoneInfo>? systemTimeZones;

    private EventHandler<EventArgs>? selectedTimeZoneIdChanged;
    private string? selectedTimeZoneId;
    private Dictionary<DateOnly, List<CalendarInstance>>? instancesByDate;

    private DateOnly? selectedDate;

    [Inject]
    [NotNull]
    private AuthenticationStateProvider? AuthStateProvider { get; set; }

    [Inject]
    [NotNull]
    private ITimeZoneProvider? TimeZoneProvider { get; set; }

    [Inject]
    [NotNull]
    private IServiceScopeFactory? ServiceScopeFactory { get; set; }

    private string? _userId;

    private void ToggleTimeline(DateOnly clickedDate)
    {
        if (selectedDate == clickedDate)
            selectedDate = null; // Clicking the same clock twice closes the tray
        else
            selectedDate = clickedDate;
    }

    protected override async Task OnInitializedAsync()
    {
        // Load the list of system time zones for the selector
        try
        {
            systemTimeZones = TimeZoneInfo.GetSystemTimeZones();
        }
        catch
        {
            systemTimeZones = Enumerable.Empty<TimeZoneInfo>();
        }

        TimeZoneProvider.LocalTimeZoneChanged += async (sender, args) =>
        {
            Logger.LogInformation("Local time zone changed to {TimeZone}", TimeZoneProvider.LocalTimeZone?.Id);
            if (TimeZoneProvider.LocalTimeZone is not null)
            {
                var browserTimeZoneId = TimeZoneProvider.LocalTimeZone.Id;
                await SetTimeZoneId(browserTimeZoneId);
            }
        };

        var initialTimeZoneId = TimeZoneInfo.Utc.Id;
        await SetTimeZoneId(initialTimeZoneId);
    }

    private async Task SetTimeZoneId(string? timeZoneId)
    {
        if (selectedTimeZoneId != timeZoneId)
        {
            if (timeZoneId is null)
            {
                Logger.LogWarning("Attempted to set time zone ID to null.");
                return;
            }
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var internalId = systemTimeZones?.FirstOrDefault(tz => tz.StandardName == timeZone.StandardName)?.Id;
            selectedTimeZoneId = internalId ?? timeZoneId;
            selectedTimeZoneIdChanged?.Invoke(this, EventArgs.Empty);
            await RefreshInstancesByDate();
        }
    }

    private async Task RefreshInstancesByDate()
    {
        try
        {
            instancesByDate = new();
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is { IsAuthenticated: true })
            {
                // Extract the unique User ID from claims
                _userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                using var scope = ServiceScopeFactory.CreateScope();
                var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();

                var timeZoneId = selectedTimeZoneId ?? TimeZoneProvider.LocalTimeZone?.Id ?? TimeZoneInfo.Utc.Id;
                var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var instancesForMonth = await dataService.GetInstancesForMonthAsync(_userId, currentMonth.Year, currentMonth.Month, localTimeZone);

                var query =
                    from x in instancesForMonth
                    group x by DateOnly.FromDateTime(ToDateTimeOffset(x.Timestamp).Date) into g
                    select g;

                instancesByDate = new Dictionary<DateOnly, List<CalendarInstance>>();
                foreach (var group in query)
                {
                    instancesByDate[group.Key] = group.ToList();
                }

                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing instances by date");
        }
    }

    private Dictionary<TimeOnly, List<CalendarInstance>>? GroupInstancesByHour(List<CalendarInstance> instances)
    {
        if (instances == null)
            return null;

        var instancesByHour = (
            from instance in instances
            group instance by new TimeOnly(ToDateTimeOffset(instance.Timestamp).Hour, 0) into g
            select g
            ).ToDictionary(
                g => g.Key,
                g => g.ToList()
            );
        return instancesByHour;
    }

    private async Task PreviousMonth()
    {
        currentMonth = currentMonth.AddMonths(-1);
        await RefreshInstancesByDate();
    }

    private async Task NextMonth()
    {
        currentMonth = currentMonth.AddMonths(1);
        await RefreshInstancesByDate();
    }

    private async Task GoToToday()
    {
        var wantedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        if (currentMonth != wantedMonth)
        {
            currentMonth = wantedMonth;
            await RefreshInstancesByDate();
        }
    }

    private DateTimeOffset ToDateTimeOffset(DateTimeOffset dateTime)
    {
        var timeZoneId = selectedTimeZoneId ?? TimeZoneProvider.LocalTimeZone?.Id ?? TimeZoneInfo.Utc.Id;
        var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return dateTime.ToOffset(localTimeZone?.GetUtcOffset(dateTime.DateTime) ?? TimeSpan.Zero);
    }
}