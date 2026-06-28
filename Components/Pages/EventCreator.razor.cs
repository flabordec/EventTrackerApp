using EventTrackerApp.Data;
using EventTrackerApp.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;


namespace EventTrackerApp.Components.Pages;

public partial class EventCreator
{
    [Inject]
    private AppDbContext DbContext { get; set; } = default!;

    private List<EventViewModel>? eventsList;

    // ViewModels for Form Binding
    private EventViewModel newEventModel = new();
    private EventValueViewModel newEventValueModel = new();

    // Selection State for logging instances
    private EventValueViewModel? selectedEventValue;
    private string selectedEventParentName = string.Empty;
    private DateTime instanceTimestamp = DateTime.Now;
    private string instanceDetails = string.Empty;

    private string? feedbackMessage;
    private bool isError;

    private bool CanSaveEvent => !string.IsNullOrWhiteSpace(newEventModel.Name) && newEventModel.Values.Any();

    protected override async Task OnInitializedAsync()
    {
        await LoadEvents();
    }

    private async Task LoadEvents()
    {
        // Include Values so we can render the child buttons
        eventsList = await DbContext.Events
            .Include(e => e.Values)
            .AsNoTracking()
            .Select(e => e.ToViewModel())
            .ToListAsync();
    }

    private void AddValueToList()
    {
        ClearFeedback();
        if (newEventValueModel.IsValid())
        {
            newEventValueModel.Index = newEventModel.Values.Count;
            newEventModel.Values.Add(newEventValueModel);
            newEventValueModel = new();
        }
        else
        {
            isError = true;
            feedbackMessage = $"Please fill in all required fields for the event value, name: {newEventValueModel.Name}.";
        }
    }

    private string? expandedEventName = null;

    private void ToggleAccordion(string? eventName)
    {
        if (expandedEventName == eventName)
            expandedEventName = null;
        else
            expandedEventName = eventName;
    }

    private void RemoveValueFromList(EventValueViewModel val)
    {
        newEventModel.Values.Remove(val);
    }

    private async Task SaveNewEvent()
    {
        if (!CanSaveEvent)
            return;

        // Map ViewModel to strict domain entities
        int index = 0;
        var newEvent = new Event
        {
            Name = newEventModel.Name.Trim(),
            Image = newEventModel.Image.Trim(),
            Values = newEventModel.Values.Select(value => new EventValue
            {
                Index = ++index,
                Name = value.Name.Trim(),
                ForegroundColor = value.ForegroundColor,
                BackgroundColor = value.BackgroundColor,
                EventId = "" // EF Core will automatically patch this FK upon saving the parent graph
            }).ToList()
        };

        DbContext.Events.Add(newEvent);
        await DbContext.SaveChangesAsync();

        // Reset form & refresh UI
        newEventModel = new();
        await LoadEvents();
    }

    private void ClearFeedback()
    {
        feedbackMessage = null;
        isError = false;
    }

    private void SelectValueForLogging(string parentName, EventValueViewModel ev)
    {
        ClearFeedback(); // Clear old feedback when starting a new log
        selectedEventParentName = parentName;
        selectedEventValue = ev;
        instanceTimestamp = DateTime.Now;
        instanceDetails = string.Empty;
    }

    private async Task SaveInstance()
    {
        ClearFeedback();

        try
        {
            if (selectedEventValue?.Id is null)
            {
                throw new ArgumentException("The selected event value is invalid.");
            }

            instanceDetails = instanceDetails?.Trim() ?? string.Empty;
            var instance = new EventInstance
            {
                Timestamp = instanceTimestamp,
                Details = instanceDetails.Trim(),
                EventValueId = selectedEventValue.Id
            };

            DbContext.Set<EventInstance>().Add(instance);
            await DbContext.SaveChangesAsync();

            // Set Success State
            isError = false;
            feedbackMessage = $"Logged '{selectedEventValue.Name}' for {selectedEventParentName} successfully.";

            // Close the logging panel
            selectedEventValue = null;
        }
        catch (Exception ex)
        {
            isError = true;
            feedbackMessage = $"Failed to save the instance to the database: {ex.Message}";
        }
    }
}
