using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using EventTrackerApp.Data;
using EventTrackerApp.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;


namespace EventTrackerApp.Components.Pages;

public partial class EventCreator
{
    [Inject]
    private AppDbContext DbContext { get; set; } = default!;

    [Inject]
    [NotNull]
    private AuthenticationStateProvider? AuthStateProvider { get; set; }
    private string? _userId;


    private List<EventViewModel>? eventsList;

    // ViewModels for Form Binding
    private EventViewModel newEventModel = new();
    private EventValueViewModel newEventValueModel = new();

    private string? feedbackMessage;
    private bool isError;

    private bool CanSaveEvent => !string.IsNullOrWhiteSpace(newEventModel.Name) && newEventModel.Values.Any();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is { IsAuthenticated: true })
        {
            // Extract the unique User ID from claims
            _userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await LoadEvents();
        }
    }

    private async Task LoadEvents()
    {
        // Include Values so we can render the child buttons
        eventsList = await DbContext.Events
            .Include(e => e.Values)
            .Where(e => e.UserId == _userId)
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

    private void RemoveValueFromList(EventValueViewModel val)
    {
        newEventModel.Values.Remove(val);
    }

    private async Task SaveNewEvent()
    {
        if (!CanSaveEvent)
            return;

        Debug.Assert(_userId is not null, "User ID should not be null when saving a new event.");

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
            }).ToList(),
            UserId = _userId
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
}
