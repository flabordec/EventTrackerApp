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
    private TimeZoneInfo? localTimeZone;
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
        TimeZoneProvider.LocalTimeZoneChanged += async (sender, args) =>
        {
            Logger.LogInformation("Local time zone changed to {TimeZone}", TimeZoneProvider.LocalTimeZone?.Id);
            if (TimeZoneProvider.LocalTimeZone is not null)
            {
                localTimeZone = TimeZoneProvider.LocalTimeZone;
            }
            await RefreshInstancesByDate();
            StateHasChanged();
        };
    }

    private async Task RefreshInstancesByDate()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is { IsAuthenticated: true })
        {
            // Extract the unique User ID from claims
            _userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            using var scope = ServiceScopeFactory.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
            instancesByDate = await dataService.GroupInstancesForMonthAsync(_userId, currentMonth.Year, currentMonth.Month, localTimeZone);
        }
    }

    private Dictionary<TimeOnly, List<CalendarInstance>>? GroupInstancesByHour(List<CalendarInstance> instances)
    {
        if (instances == null)
            return null;

        var instancesByHour = (
            from instance in instances
            group instance by new TimeOnly(instance.Timestamp.Hour, 0) into g
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
}