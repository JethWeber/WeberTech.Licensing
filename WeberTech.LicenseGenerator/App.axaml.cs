using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
            // ⚠️ TEMPORÁRIO — Fase 6, checkpoint do M1 (fundação visual).
            // Mostra só a DesignSystemPreview para confirmar visualmente que
            // os DesignTokens/Styles novos renderizam certo, antes de avançar
            // para o M2 (Login/Register, que aí sim vira a entrada real da
            // app). O código do M0 (ActivationView) fica comentado abaixo,
            // intacto, para religar quando a navegação for reconstruída no M3.
            desktop.MainWindow = new Window
            {
                Title = "WeberTech.LicenseGenerator — M1 (verificação visual)",
                Width = 900,
                Height = 700,
                Content = new DesignSystemPreview()
            };

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
