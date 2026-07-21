using System.ComponentModel.DataAnnotations.Schema;
using BlazorComponentUtilities;

namespace EventTrackerApp.Data;

using System.Diagnostics;
using System.Drawing;
using Microsoft.AspNetCore.Identity;

internal class ApplicationUser : IdentityUser
{
    public ICollection<Event> Events { get; set; } = new List<Event>();
}

[DebuggerDisplay("Event: {Name}")]
internal class Event
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; init; }
    public required string Name { get; init; }
    public required string Image { get; init; }

    public ApplicationUser? User { get; init; }
    public required string UserId { get; init; }

    public ICollection<EventValue> Values { get; set; } = new List<EventValue>();
}

[DebuggerDisplay("EventValue: {Name}")]
internal class EventValue
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; set; }
    public required int Index { get; init; }
    public required string Name { get; init; }
    public required string ForegroundColor { get; init; }
    public required string BackgroundColor { get; init; }

    public required string EventId { get; init; }
    public Event? Event { get; init; }

    public ICollection<EventInstance> Instances { get; set; } = new List<EventInstance>();
}

[DebuggerDisplay("EventInstance: {Timestamp}")]
internal class EventInstance
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; init; }
    public required DateTimeOffset Timestamp { get; set; }
    public required string Details { get; init; }

    public required string EventValueId { get; init; }
    public EventValue? EventValue { get; init; }
}