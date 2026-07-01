using System.Diagnostics.CodeAnalysis;
using EventTrackerApp.Data;
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
    [Inject]
    private IDataService _dataService { get; set; } = default!;

    // Calendar State
    private DateTime currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private Dictionary<DateOnly, List<CalendarInstance>>? instancesByDate;

    private DateOnly? selectedDate;

    [Inject]
    [NotNull]
    private AuthenticationStateProvider? AuthStateProvider { get; set; }
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
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is { IsAuthenticated: true })
        {
            // Extract the unique User ID from claims
            _userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            instancesByDate = await _dataService.GroupInstancesForMonthAsync(_userId, currentMonth);
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
        instancesByDate = await _dataService.GroupInstancesForMonthAsync(_userId, currentMonth);
    }

    private async Task NextMonth()
    {
        currentMonth = currentMonth.AddMonths(1);
        instancesByDate = await _dataService.GroupInstancesForMonthAsync(_userId, currentMonth);
    }

    private async Task GoToToday()
    {
        var wantedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        if (currentMonth != wantedMonth)
        {
            currentMonth = wantedMonth;
            instancesByDate = await _dataService.GroupInstancesForMonthAsync(_userId, currentMonth);
        }
    }
}