using System.ComponentModel.DataAnnotations;
using EventTrackerApp.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using EventTrackerApp.Data;
using System.Diagnostics.CodeAnalysis;

namespace EventTrackerApp.Components.Pages.Account;

public partial class Login
{
    [Inject]
    [NotNull]
    private SignInManager<ApplicationUser>? SignInManager { get; set; }
    [Inject]
    [NotNull]
    private ILogger<Login>? Logger { get; set; }
    [Inject]
    [NotNull]
    private NavigationManager? NavigationManager { get; set; }


    [SupplyParameterFromForm]
    public InputModel? Input { get; set; }

    [CascadingParameter]
    public HttpContext HttpContext { get; set; } = default!;

    private string? ErrorMessage;

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        if (HttpMethods.IsGet(HttpContext.Request.Method))
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    public async Task LoginUser()
    {
        if (Input == null)
        {
            ErrorMessage = "Invalid login attempt.";
            return;
        }

        var result = await SignInManager.PasswordSignInAsync(Input.Email, Input.Password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            Logger.LogInformation("User logged in.");
            NavigationManager.NavigateTo("/", forceLoad: true); // forceLoad is critical here to refresh the auth state across the app
        }
        else
        {
            ErrorMessage = "Invalid login attempt.";
        }
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}