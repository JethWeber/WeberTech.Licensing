using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Services;

namespace WeberTech.LicenseGenerator.ViewModels;

public sealed partial class RegisterViewModel : ObservableObject
{
    private readonly AuthService _authService;

    public event EventHandler<User>? RegisterSucceeded;
    public event EventHandler? LoginRequested;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public RegisterViewModel() : this(new AuthService()) { }

    public RegisterViewModel(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        ErrorMessage = null;

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "As passwords não coincidem.";
            return;
        }

        IsBusy = true;
        try
        {
            AuthResult result = await _authService.RegisterAsync(Username, DisplayName, Password);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            RegisterSucceeded?.Invoke(this, result.User!);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoToLogin() => LoginRequested?.Invoke(this, EventArgs.Empty);
}
