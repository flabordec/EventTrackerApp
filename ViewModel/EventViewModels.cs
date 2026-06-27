using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BlazorComponentUtilities;

namespace EventTrackerApp.ViewModel;

public class EventViewModel
{
    public string? Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Image { get; set; } = string.Empty;

    public List<EventValueViewModel> Values { get; set; } = new();
}

public class EventValueViewModel
{
    public string? Id { get; set; }
    [Required]
    public int Index { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    public string ForegroundColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;

    public string Style =>
        StyleBuilder
            .Default("")
            .AddStyle("color", ForegroundColor, !string.IsNullOrWhiteSpace(ForegroundColor))
            .AddStyle("background-color", BackgroundColor, !string.IsNullOrWhiteSpace(BackgroundColor))
            .Build();


    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name);
    }
}

public class EventInstanceViewModel
{
    public string? Id { get; set; }
    [Required]
    public DateTime Timestamp { get; set; }
    [Required]
    public string Details { get; set; } = string.Empty;
}