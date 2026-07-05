using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using ApexCharts;
using EventTrackerApp.Data;
using EventTrackerApp.Helpers;
using EventTrackerApp.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EventTrackerApp.Components.Pages;

public record HistogramSeries(string EventName, string SeriesColorHtml, HistogramValues[] Values);

public record HistogramValues(string TimestampString, int Value);

public partial class EventViewer
{
    [Inject]
    [NotNull]
    private ILogger<EventViewer>? Logger { get; set; }

    // Calendar State
    private DateTime currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private IEnumerable<TimeZoneInfo>? systemTimeZones;

    public EventHandler<EventArgs>? selectedTimeZoneIdChanged;
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

    private string? selectedEventNameForCharts;
    private ApexChart<HistogramValues>? eventsChart;
    private ApexChartOptions<HistogramValues>? eventsChartOptions;
    private Dictionary<string, List<HistogramSeries>> histogramsByEventName = new();


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

        eventsChartOptions = new ApexChartOptions<HistogramValues>()
        {
            Chart = new()
            {
                Stacked = true,
            },
            Theme = new()
            {
                Mode = Mode.Dark
            }
        };

        TimeZoneProvider.LocalTimeZoneChanged += async (sender, args) =>
        {
            Logger.LogInformation("Local time zone changed to {TimeZone}", TimeZoneProvider.LocalTimeZone?.Id);
            if (TimeZoneProvider.LocalTimeZone is not null)
            {
                var browserTimeZoneId = TimeZoneProvider.LocalTimeZone.Id;
                await SetTimeZoneId(browserTimeZoneId);
            }
        };
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
                instancesByDate = GroupByDate(instancesForMonth);

                histogramsByEventName = CalculateHistogramsByEventName(instancesForMonth);
                if (eventsChart is not null)
                {
                    await eventsChart.UpdateSeriesAsync(true);
                }

                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing instances by date");
        }
    }

    private async Task RefreshCharts()
    {
        if (eventsChart is not null)
        {
            await eventsChart.UpdateSeriesAsync(true);
        }
    }

    private decimal TimestampToDecimal(DateTimeOffset timestamp)
    {
        decimal v = timestamp.Hour;
        v += timestamp.Minute / 60.0m;
        v += timestamp.Second / 60.0m / 60.0m;
        v += timestamp.Millisecond / 60.0m / 60.0m / 1000.0m;
        return v;
    }

    private string DecimalHoursToTimestampString(decimal hoursFractional)
    {
        var timestamp = TimeOnly.FromTimeSpan(TimeSpan.FromHours((double)hoursFractional));
        return timestamp.ToString("h tt");
    }

    private Dictionary<string, List<HistogramSeries>> CalculateHistogramsByEventName(
        List<CalendarInstance> instancesForMonth)
    {
        var results = new Dictionary<string, List<HistogramSeries>>();

        var instancesByEventName = instancesForMonth.GroupBy(x => x.EventName);
        foreach (var instancesForCurrentEvent in instancesByEventName)
        {
            var eventName = instancesForCurrentEvent.Key;
            var currentHistograms = new List<HistogramSeries>();

            foreach (var g in instancesForCurrentEvent.GroupBy(x => (x.ValueName, x.ColorHtml)))
            {

                (string eventValueName, string colorHtml) = g.Key;
                var timestamps = g.Select(x => TimestampToDecimal(x.Timestamp)).ToArray();

                int numBuckets = 12;
                decimal min = 0;
                decimal max = 24;

                decimal bucketSize = (max - min) / numBuckets;

                var buckets = new int[numBuckets];
                var edges = (
                    from i in Enumerable.Range(0, numBuckets + 1)
                    select min + i * bucketSize
                    ).ToArray();

                foreach (var x in timestamps)
                {
                    int index = (int)((x - min) / bucketSize);
                    if (index == numBuckets)
                        index = numBuckets - 1;
                    buckets[index] += 1;
                }
                var bucketsObj = buckets.Select(
                    (x, i) => new HistogramValues(
                        DecimalHoursToTimestampString(edges[i]),
                        x)
                    ).ToArray();
                currentHistograms.Add(new HistogramSeries(eventValueName, colorHtml, bucketsObj));
            }
            results.Add(eventName, currentHistograms);
        }
        return results;
    }

    private Dictionary<DateOnly, List<CalendarInstance>> GroupByDate(List<CalendarInstance> instancesForMonth)
    {
        var query =
            from x in instancesForMonth
            group x by DateOnly.FromDateTime(ToClientTime(x.Timestamp).Date) into g
            select g;

        var instancesByDate = new Dictionary<DateOnly, List<CalendarInstance>>();
        foreach (var group in query)
        {
            instancesByDate[group.Key] = group.ToList();
        }
        return instancesByDate;
    }

    private Dictionary<TimeOnly, List<CalendarInstance>>? GroupInstancesByHour(List<CalendarInstance> instances)
    {
        if (instances == null)
            return null;

        var instancesByHour = (
            from instance in instances
            group instance by new TimeOnly(ToClientTime(instance.Timestamp).Hour, 0) into g
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

    private DateTimeOffset ToClientTime(DateTimeOffset dateTime)
    {
        var clientTimeZoneId = selectedTimeZoneId ?? TimeZoneProvider.LocalTimeZone?.Id ?? TimeZoneInfo.Utc.Id;
        var clientTimeZone = TimeZoneInfo.FindSystemTimeZoneById(clientTimeZoneId);
        return dateTime.ToOffset(clientTimeZone?.GetUtcOffset(dateTime.DateTime) ?? TimeSpan.Zero);
    }
}