using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace WeberTech.LicenseGenerator;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Placeholder da Fase 1. Substituir por ActivationView / IssueLicenseView
            // conforme o fluxo descrito na Fase 6 (Views/ActivationView.axaml etc.).
            desktop.MainWindow = new Window
            {
                Title = "WeberTech.LicenseGenerator",
                Width = 800,
                Height = 600
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
