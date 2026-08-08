using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Views;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// Shell de navegação (M3, Fase 6) — sidebar + área de conteúdo, seguindo
/// o layout dos mockups (Dashboard / Emitir Licença / Histórico /
/// Configurações). Troca <see cref="CurrentPageContent"/> por view-first
/// (mesmo padrão já usado em <c>App.axaml.cs</c> desde o M0), sem precisar
/// de um framework de navegação — ainda são poucas páginas.
/// </summary>
public sealed partial class NavigationShellViewModel : ObservableObject
{
    private readonly Action _onLogout;

    public User CurrentUser { get; }

    [ObservableProperty]
    private object? _currentPageContent;

    [ObservableProperty]
    private bool _isDashboardActive;

    [ObservableProperty]
    private bool _isIssueLicenseActive;

    [ObservableProperty]
    private bool _isHistoryActive;

    [ObservableProperty]
    private bool _isSettingsActive;

    public NavigationShellViewModel(User currentUser, Action onLogout)
    {
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _onLogout = onLogout ?? throw new ArgumentNullException(nameof(onLogout));

        NavigateToIssueLicense(); // tela mais usada no dia a dia — abre por padrão
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        SetActive(dashboard: true);
        CurrentPageContent = new PlaceholderView(
            "Dashboard",
            "Chega no M8 — métricas calculadas a partir do histórico real (M7), nada mockado aqui.");
    }

    [RelayCommand]
    private void NavigateToIssueLicense()
    {
        SetActive(issueLicense: true);
        CurrentPageContent = BuildIssueLicenseFlow();
    }

    [RelayCommand]
    private void NavigateToHistory()
    {
        SetActive(history: true);
        CurrentPageContent = new PlaceholderView(
            "Histórico de Licenças",
            "Chega no M7 — toda licença emitida no M6 aparece aqui automaticamente.");
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        SetActive(settings: true);
        CurrentPageContent = new PlaceholderView(
            "Configurações",
            "Chega no M9 — segurança/produtos. A chave privada nunca vai aparecer em texto aqui, só metadados.");
    }

    [RelayCommand]
    private void Logout() => _onLogout();

    /// <summary>
    /// Passo 1 (Fase 4/M0) já está pronto — colar/interpretar o QR. O
    /// formulário de emissão de verdade (Passo 2) só chega no M6; por
    /// agora, confirmar um pedido mostra o que foi interpretado num
    /// placeholder, para deixar claro que o fluxo termina aí por agora.
    /// </summary>
    private object BuildIssueLicenseFlow()
    {
        var activationViewModel = new ActivationViewModel();
        activationViewModel.RequestConfirmed += (_, request) =>
        {
            CurrentPageContent = new PlaceholderView(
                $"Pedido interpretado: {request.ProductId}",
                $"Machine ID {request.MachineId} — o formulário de emissão (M6) ainda não existe.");
        };

        return new ActivationView { DataContext = activationViewModel };
    }

    private void SetActive(bool dashboard = false, bool issueLicense = false, bool history = false, bool settings = false)
    {
        IsDashboardActive = dashboard;
        IsIssueLicenseActive = issueLicense;
        IsHistoryActive = history;
        IsSettingsActive = settings;
    }
}
