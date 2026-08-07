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
            var activationViewModel = new ActivationViewModel();

            // Passo 1 da Fase 6 (Secção 11 do roteiro). O Passo 2
            // (IssueLicenseView) ainda não existe — por agora, confirmar
            // um pedido só regista no output de debug, para validar o
            // fluxo de ponta a ponta desta primeira tela.
            activationViewModel.RequestConfirmed += (_, request) =>
                System.Diagnostics.Debug.WriteLine(
                    $"[ActivationView] Pedido confirmado: produto={request.ProductId}, machineId={request.MachineId}. " +
                    "Passo 2 (IssueLicenseView) ainda por implementar.");

            desktop.MainWindow = new Window
            {
                Title = "WeberTech.LicenseGenerator",
                Width = 760,
                Height = 640,
                Content = new ActivationView { DataContext = activationViewModel }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
