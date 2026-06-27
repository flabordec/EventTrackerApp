using System.ComponentModel.DataAnnotations.Schema;

namespace EventTrackerApp.Data;

public class Event
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; init; }
    public required string Name { get; init; }

    public ICollection<EventValue> Values { get; set; } = new List<EventValue>();
}

public class EventValue
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; set; }
    public required string Name { get; init; }


    public required string EventId { get; init; }
    public Event? Event { get; init; }
}

public class EventInstance
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Details { get; init; }

    public required string EventValueId { get; init; }
    public EventValue? EventValue { get; init; }
}