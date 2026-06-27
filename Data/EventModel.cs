using System.ComponentModel.DataAnnotations.Schema;
using BlazorComponentUtilities;
using EventTrackerApp.ViewModel;

namespace EventTrackerApp.Data;

public class Event
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; init; }
    public required string Name { get; init; }
    public required string Image { get; init; }

    public ICollection<EventValue> Values { get; set; } = new List<EventValue>();

    internal EventViewModel ToViewModel() => new()
    {
        Id = Id,
        Name = Name,
        Image = Image,
        Values = Values.Select(v => v.ToViewModel()).ToList()
    };
}

public class EventValue
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

    internal EventValueViewModel ToViewModel()
    {
        return new EventValueViewModel
        {
            Id = Id,
            Index = Index,
            Name = Name,
            ForegroundColor = ForegroundColor,
            BackgroundColor = BackgroundColor
        };
    }
}

public class EventInstance
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Details { get; init; }

    public required string EventValueId { get; init; }
    public EventValue? EventValue { get; init; }

    internal EventInstanceViewModel ToViewModel()
    {
        return new EventInstanceViewModel
        {
            Id = Id,
            Timestamp = Timestamp,
            Details = Details
        };
    }
}