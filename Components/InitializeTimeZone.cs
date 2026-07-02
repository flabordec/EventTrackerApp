using EventTrackerApp.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public sealed class InitializeTimeZone : ComponentBase
{
    [Inject]
    public ITimeZoneProvider TimeZoneProvider { get; set; } = default!;
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !TimeZoneProvider.IsLocalTimeZoneSet)
        {
            try
            {
                await using var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./timezone.js");
                var timeZone = await module.InvokeAsync<string>("getBrowserTimeZone");
                TimeZoneProvider.SetBrowserTimeZone(timeZone);
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}