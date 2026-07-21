using System.ComponentModel.DataAnnotations.Schema;
using BlazorComponentUtilities;

namespace EventTrackerApp.Data;

using System.Diagnostics;

[DebuggerDisplay("Event: {Name}")]
public record EventDto(string? Id, string Name, string Image, string UserId, List<EventValueDto>? Values = null);

[DebuggerDisplay("EventValue: {Name}")]
public record EventValueDto(string? Id, int Index, string Name, string ForegroundColor, string BackgroundColor, string EventId = "", List<EventInstanceDto>? Instances = null);

[DebuggerDisplay("EventInstance: {Timestamp}")]
public record EventInstanceDto(string? Id, DateTimeOffset Timestamp, string Details, string EventValueId = "");

[DebuggerDisplay("Calendar instance: {Timestamp} {EventName} {EventValueName}")]
public record CalendarEventInstanceDto(DateTimeOffset Timestamp, string EventName, string EventValueName, string Image, string ForegroundColor, string BackgroundColor)
{
    public string Style =>
        StyleBuilder
            .Default("")
            .AddStyle("color", ForegroundColor, !string.IsNullOrWhiteSpace(ForegroundColor))
            .AddStyle("background-color", BackgroundColor, !string.IsNullOrWhiteSpace(BackgroundColor))
            .Build();
}