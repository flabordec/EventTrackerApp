using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using EventTrackerApp.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace EventTrackerApp.Components.Pages.Account;

public partial class Register
{
    [Inject]
    [NotNull]
    private UserManager<ApplicationUser>? UserManager { get; set; }
    [Inject]
    [NotNull]
    private IUserStore<ApplicationUser>? UserStore { get; set; }
    [Inject]
    [NotNull]
    private SignInManager<ApplicationUser>? SignInManager { get; set; }
    [Inject]
    [NotNull]
    private ILogger<Register>? Logger { get; set; }
    [Inject]
    [NotNull]
    private NavigationManager? NavigationManager { get; set; }


    [SupplyParameterFromForm]
    public InputModel? Input { get; set; }

    private IEnumerable<IdentityError> IdentityErrors = Enumerable.Empty<IdentityError>();

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();
    }

    public async Task RegisterUser()
    {
        if (Input == null)
        {
            IdentityErrors = new List<IdentityError>
            {
                new IdentityError { Description = "Invalid registration attempt." }
            };
            return;
        }

        var user = new ApplicationUser();

        // EF Core Identity uses the UserStore to set the username/email securely
        await UserStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);

        var emailStore = (IUserEmailStore<ApplicationUser>)UserStore;
        await emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

        var result = await UserManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            Logger.LogInformation("User created a new account with password.");

            await SignInManager.SignInAsync(user, isPersistent: false);
            NavigationManager.NavigateTo("/", forceLoad: true);
        }
        else
        {
            IdentityErrors = result.Errors;
        }
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}