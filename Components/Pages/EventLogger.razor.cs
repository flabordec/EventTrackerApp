using System.Diagnostics.CodeAnalysis;
using EventTrackerApp.Data;
using EventTrackerApp.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;


namespace EventTrackerApp.Components.Pages;

public partial class EventLogger
{
    [Inject]
    [NotNull]
    private ILogger<EventViewer>? Logger { get; set; }
    [Inject]
    private AppDbContext DbContext { get; set; } = default!;

    private List<EventViewModel>? eventsList;

    // ViewModels for Form Binding
    private EventViewModel newEventModel = new();
    private EventValueViewModel newEventValueModel = new();

    // Selection State for logging instances
    private EventValueViewModel? selectedEventValue;
    private string selectedEventParentName = string.Empty;
    private DateTime instanceTimestamp = DateTime.UtcNow;
    private string instanceDetails = string.Empty;

    private string? feedbackMessage;
    private bool isError;

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

    private string? expandedEventName = null;

    private void ToggleAccordion(string? eventName)
    {
        if (expandedEventName == eventName)
            expandedEventName = null;
        else
            expandedEventName = eventName;
    }

    private void ClearFeedback()
    {
        feedbackMessage = null;
        isError = false;
    }

    private async Task SaveInstance(string parentName, EventValueViewModel ev)
    {
        ClearFeedback();

        try
        {
            selectedEventParentName = parentName;
            selectedEventValue = ev;
            instanceTimestamp = DateTime.UtcNow;

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
            feedbackMessage = "Failed to save the instance to the database";
            Logger.LogError(ex, "Failed to save the instance to the database");
        }
    }
}
