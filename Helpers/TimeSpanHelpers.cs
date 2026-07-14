namespace EventTrackerApp.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;

public static class TimeSpanExtensions
{
    public static string ToHumanReadableString(this TimeSpan timeSpan, int partsToTake = 2)
    {
        // Use Duration() to ensure we are working with positive values
        // in case a negative TimeSpan is passed in.
        var duration = timeSpan.Duration();

        if (duration.TotalMilliseconds == 0)
        {
            return "0 seconds";
        }

        var parts = new List<string>();

        if (duration.Days > 0)
            parts.Add($"{duration.Days} day{(duration.Days == 1 ? "" : "s")}");

        if (duration.Hours > 0)
            parts.Add($"{duration.Hours} hour{(duration.Hours == 1 ? "" : "s")}");

        if (duration.Minutes > 0)
            parts.Add($"{duration.Minutes} minute{(duration.Minutes == 1 ? "" : "s")}");

        if (duration.Seconds > 0)
            parts.Add($"{duration.Seconds} second{(duration.Seconds == 1 ? "" : "s")}");

        if (duration.Milliseconds > 0)
            parts.Add($"{duration.Milliseconds} millisecond{(duration.Milliseconds == 1 ? "" : "s")}");

        // Limit to at most N parts and join them with commas
        return string.Join(", ", parts.Take(partsToTake));
    }
}