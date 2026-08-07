using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Services;
using WeberTech.LicenseGenerator.ViewModels;
using WeberTech.LicenseGenerator.Views;

namespace WeberTech.LicenseGenerator;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new Window
            {
                Title = "WeberTech.LicenseGenerator",
                Width = 1000,
                Height = 700
            };

            var authService = new AuthService();

            // Verificação local rápida (SQLite) — aceitável bloquear o
            // arranque por isto; nada mais pode ser mostrado antes de saber
            // se existe algum utilizador (decide Login vs. Register).
            authService.EnsureDatabaseCreatedAsync().GetAwaiter().GetResult();
            bool hasAnyUser = authService.HasAnyUserAsync().GetAwaiter().GetResult();

            void ShowLogin()
            {
                var loginViewModel = new LoginViewModel(authService);
                loginViewModel.LoginSucceeded += (_, user) => ShowAuthenticatedArea(user);
                loginViewModel.RegisterRequested += (_, _) => ShowRegister();
                mainWindow.Content = new LoginView { DataContext = loginViewModel };
            }

            void ShowRegister()
            {
                var registerViewModel = new RegisterViewModel(authService);
                registerViewModel.RegisterSucceeded += (_, user) => ShowAuthenticatedArea(user);
                registerViewModel.LoginRequested += (_, _) => ShowLogin();
                mainWindow.Content = new RegisterView { DataContext = registerViewModel };
            }

            void ShowAuthenticatedArea(User user)
            {
                // M3 (shell de navegação) ainda não existe — por agora, área
                // autenticada provisória, só para provar que o gate de login
                // funciona de ponta a ponta (incluindo o logout).
                mainWindow.Content = new AuthenticatedPlaceholderView(user, ShowLogin);
            }

            if (hasAnyUser)
                ShowLogin();
            else
                ShowRegister(); // bootstrap: primeiro utilizador da ferramenta

            desktop.MainWindow = mainWindow;

            // Código do M0/M1 (ActivationView/DesignSystemPreview isoladas)
            // fica comentado abaixo, intacto, para religar no M3/M6.
            //
            // desktop.MainWindow = new Window
            // {
            //     Title = "WeberTech.LicenseGenerator — M1 (verificação visual)",
            //     Width = 900,
            //     Height = 700,
            //     Content = new DesignSystemPreview()
            // };
            //
            // var activationViewModel = new ActivationViewModel();
            // activationViewModel.RequestConfirmed += (_, request) =>
            // {
            //     var issueViewModel = new IssueLicenseViewModel(
            //         request,
            //         new LicenseIssuer(new SignatureService()),
            //         new KeyProvider(),
            //         new FileDialogService(() => desktop.MainWindow));
            //     desktop.MainWindow!.Content = new IssueLicenseView { DataContext = issueViewModel };
            // };
            // desktop.MainWindow = new Window
            // {
            //     Title = "WeberTech.LicenseGenerator",
            //     Width = 760,
            //     Height = 640,
            //     Content = new ActivationView { DataContext = activationViewModel }
            // };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
