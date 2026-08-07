using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Services;

namespace WeberTech.LicenseGenerator.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    public event EventHandler<User>? LoginSucceeded;
    public event EventHandler? RegisterRequested;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public LoginViewModel() : this(new AuthService()) { }

    public LoginViewModel(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Preenche utilizador e password.";
            return;
        }

        IsBusy = true;
        try
        {
            AuthResult result = await _authService.AuthenticateAsync(Username, Password);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            LoginSucceeded?.Invoke(this, result.User!);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoToRegister() => RegisterRequested?.Invoke(this, EventArgs.Empty);
}
