using Avalonia.Controls;
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
    private readonly Func<TopLevel?> _topLevelProvider;

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

    public NavigationShellViewModel(User currentUser, Action onLogout, Func<TopLevel?> topLevelProvider)
    {
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _onLogout = onLogout ?? throw new ArgumentNullException(nameof(onLogout));
        _topLevelProvider = topLevelProvider ?? throw new ArgumentNullException(nameof(topLevelProvider));

        NavigateToIssueLicense(); // tela mais usada no dia a dia — abre por padrão
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        SetActive(dashboard: true);
        CurrentPageContent = new DashboardView { DataContext = new DashboardViewModel() };
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
        CurrentPageContent = new HistoryView { DataContext = new HistoryViewModel() };
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        SetActive(settings: true);
        CurrentPageContent = new SettingsView { DataContext = new SettingsViewModel() };
    }

    [RelayCommand]
    private void Logout() => _onLogout();

    /// <summary>
    /// Passo 1 (Fase 4/M0) — colar/interpretar o QR. Ao confirmar um
    /// pedido, mostra o formulário de emissão completo (M6):
    /// CustomerPicker (M4) + ProductProfilePicker (M5) + plano/tipo/datas/
    /// módulos + LicenseIssuer real (Core, Fase 5), gerando um .wta de
    /// verdade em disco.
    /// </summary>
    private object BuildIssueLicenseFlow()
    {
        var activationViewModel = new ActivationViewModel(_topLevelProvider);
        activationViewModel.RequestConfirmed += (_, request) =>
        {
            var issueViewModel = new IssueLicenseViewModel(request, _topLevelProvider);
            CurrentPageContent = new IssueLicenseView { DataContext = issueViewModel };
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
