namespace EventTrackerApp.Helpers;

public interface ITimeZoneProvider
{
    event EventHandler? LocalTimeZoneChanged;
    TimeZoneInfo? LocalTimeZone { get; }
    bool IsLocalTimeZoneSet { get; }
    void SetBrowserTimeZone(string timeZone);
}

internal sealed class BrowserTimeZoneProvider : ITimeZoneProvider
{
    // Notify when the local time zone changes
    public event EventHandler? LocalTimeZoneChanged;

    public TimeZoneInfo? LocalTimeZone { get; private set; }

    public bool IsLocalTimeZoneSet => LocalTimeZone != null;

    // Set the local time zone
    public void SetBrowserTimeZone(string timeZone)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var timeZoneInfo))
        {
            timeZoneInfo = null;
        }

        if (timeZoneInfo != LocalTimeZone)
        {
            LocalTimeZone = timeZoneInfo;
            LocalTimeZoneChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}