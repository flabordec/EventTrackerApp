using EventTrackerApp.Data;
using EventTrackerApp.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;


namespace EventTrackerApp.Components.Pages;

public partial class Viewer
{
    [Inject]
    private AppDbContext DbContext { get; set; } = default!;
    private List<EventViewModel>? eventsList;

    protected override async Task OnInitializedAsync()
    {
        await LoadEvents();
    }
    private async Task LoadEvents()
    {
        // Include Values so we can render the child buttons
        eventsList = await DbContext.Events
            .Include(e => e.Values)
            .ThenInclude(ev => ev.Instances)
            .AsNoTracking()
            .Select(e => e.ToViewModel())
            .ToListAsync();
    }

}