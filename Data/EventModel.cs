using System.ComponentModel.DataAnnotations.Schema;

namespace EventTrackerApp.Data;

public class Event
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; set; }
    public string Name { get; set; }

    public Event()
        : this(string.Empty)
    {

    }

    public Event(string name)
    {
        Name = name;
    }
}

public class EventInstance
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Details { get; set; }

    public string EventId { get; set; }
    public Event? Event { get; set; }

    public EventInstance()
        : this(null, string.Empty)
    {
    }

    public EventInstance(Event? eventObj, string details)
    {
        Timestamp = DateTime.UtcNow;
        Event = eventObj;
        Details = details;
        EventId = eventObj?.Id ?? string.Empty;
    }
}