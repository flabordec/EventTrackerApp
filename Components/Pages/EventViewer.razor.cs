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


public record EventStats(
    List<HistogramSeries> HistogramSeries,
    int TotalDays,
    double AverageEventsPerDay,
    TimeSpan AverageTimeBetweenEvents
);

public record HistogramSeries(
    string EventName,
    string SeriesColorHtml,
    HistogramValues[] Values);

public record HistogramValues(string TimestampString, int Value, decimal ValuePerDay);

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

    [Inject]
    [NotNull]
    public IApexChartService? ApexChartService { get; set; }
    private Dictionary<string, EventStats> eventStatsByEventName = new();

    private bool showEventDates;

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
            instancesByDate = null;
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

                eventStatsByEventName = CalculateHistogramsByEventName(instancesForMonth);

                StateHasChanged();
                await RefreshCharts();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing instances by date");
        }
    }

    private async Task RefreshCharts()
    {
        foreach (var eventsChart in ApexChartService.Charts)
        {
            if (eventsChart is ApexChart<HistogramValues> histogramChart)
            {
                await histogramChart.UpdateSeriesAsync(false);
            }
        }
    }

    private decimal TimestampToDecimal(DateTimeOffset timestamp)
    {
        var clientTimestamp = ToClientTime(timestamp);
        decimal v = clientTimestamp.Hour;
        v += clientTimestamp.Minute / 60.0m;
        v += clientTimestamp.Second / 60.0m / 60.0m;
        v += clientTimestamp.Millisecond / 60.0m / 60.0m / 1000.0m;
        return v;
    }

    private string DecimalHoursToTimestampString(decimal hoursFractional)
    {
        var timestamp = TimeOnly.FromTimeSpan(TimeSpan.FromHours((double)hoursFractional));
        return timestamp.ToString("h tt");
    }

    private Dictionary<string, EventStats> CalculateHistogramsByEventName(
        List<CalendarInstance> instancesForMonth)
    {
        var results = new Dictionary<string, EventStats>();

        var instancesByEventName = instancesForMonth.GroupBy(x => x.EventName);
        foreach (var instancesForCurrentEvent in instancesByEventName)
        {
            var eventName = instancesForCurrentEvent.Key;

            double averageMillisecondsBetweenEvents = 0.0;
            var instancesForCurrentEventArray = instancesForCurrentEvent.OrderBy(x => x.Timestamp).ToArray();
            for (int i = 1; i < instancesForCurrentEventArray.Length; i++)
            {
                var timeBetween = instancesForCurrentEventArray[i].Timestamp - instancesForCurrentEventArray[i - 1].Timestamp;
                var millisecondsBetweenEvents = timeBetween.TotalMilliseconds;
                averageMillisecondsBetweenEvents += (millisecondsBetweenEvents / instancesForCurrentEventArray.Length);
            }
            TimeSpan averageTimeBetweenEvents = TimeSpan.FromMilliseconds(averageMillisecondsBetweenEvents);

            var currentHistograms = new List<HistogramSeries>();
            var instancesGroupedByDay = (
                from x in instancesForCurrentEvent
                let timestamp = ToClientTime(x.Timestamp)
                where timestamp.Date != DateTime.Today
                group x by timestamp.Date
                );
            var average = instancesGroupedByDay.Average(x => x.Count());
            var totalDays = instancesGroupedByDay.Count();

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
                        x,
                        (decimal)x / totalDays)
                    ).ToArray();
                currentHistograms.Add(new HistogramSeries(eventValueName, colorHtml, bucketsObj));
            }
            results.Add(eventName, new(currentHistograms, totalDays, average, averageTimeBetweenEvents));
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