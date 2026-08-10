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
            var productProfileService = new ProductProfileService();

            // Verificação local rápida (SQLite) — aceitável bloquear o
            // arranque por isto; nada mais pode ser mostrado antes de saber
            // se existe algum utilizador (decide Login vs. Register).
            try
            {
                authService.EnsureDatabaseCreatedAsync().GetAwaiter().GetResult();
                productProfileService.EnsureSeedDataAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
            {
                // EnsureCreated() só cria o esquema inteiro na primeira vez
                // que o ficheiro .db é gerado — não faz update incremental,
                // nem de tabelas nem de colunas novas. Acontece sempre que um
                // marco novo (Fase 6) muda o GeneratorDbContext e o
                // generator.db local já existia de uma sessão anterior.
                // Esperado durante desenvolvimento ativo (ver README).
                throw new InvalidOperationException(DatabaseErrorHelper.DescribeError(ex), ex);
            }

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
                // mainWindow já É um TopLevel (Window herda de TopLevel) —
                // passar () => mainWindow é suficiente para o FileDialogService
                // (usado dentro do formulário de emissão, M6) conseguir abrir
                // diálogos nativos de ficheiro ancorados nesta janela.
                var shellViewModel = new NavigationShellViewModel(user, ShowLogin, () => mainWindow);
                mainWindow.Content = new NavigationShellView { DataContext = shellViewModel };
            }

            if (hasAnyUser)
                ShowLogin();
            else
                ShowRegister(); // bootstrap: primeiro utilizador da ferramenta

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
