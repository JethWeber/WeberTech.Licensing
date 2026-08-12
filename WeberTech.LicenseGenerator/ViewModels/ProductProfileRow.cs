using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.LicenseGenerator.Entities;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// Linha da tabela "Perfis de Produto" (M9) — comandos por linha delegam
/// para o <c>ProductProfileSettingsViewModel</c> pai via callback (recebido
/// no construtor), em vez de "ancestor binding" em XAML dentro do
/// <c>ItemsControl</c> — mesma razão que levou o <c>HistoryRow</c> (M7/M8)
/// a calcular cores diretamente em vez de <c>Classes</c> dinâmicas.
/// </summary>
public sealed partial class ProductProfileRow : ObservableObject
{
    private readonly Func<ProductProfile, Task> _onEdit;
    private readonly Func<ProductProfile, Task> _onDeactivate;
    private readonly Func<ProductProfile, Task> _onReactivate;

    public ProductProfileRow(
        ProductProfile profile,
        Func<ProductProfile, Task> onEdit,
        Func<ProductProfile, Task> onDeactivate,
        Func<ProductProfile, Task> onReactivate)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _onEdit = onEdit ?? throw new ArgumentNullException(nameof(onEdit));
        _onDeactivate = onDeactivate ?? throw new ArgumentNullException(nameof(onDeactivate));
        _onReactivate = onReactivate ?? throw new ArgumentNullException(nameof(onReactivate));
    }

    public ProductProfile Profile { get; }

    public string FeaturesLabel => string.Join(", ", Profile.AvailableFeatures);

    public string StatusLabel => Profile.IsActive ? "ATIVO" : "INATIVO";

    public IBrush StatusBackgroundBrush => Profile.IsActive
        ? new SolidColorBrush(Color.Parse("#4D2E7D32"))
        : new SolidColorBrush(Color.Parse("#1A8B91A0"));

    public IBrush StatusForegroundBrush => Profile.IsActive
        ? new SolidColorBrush(Color.Parse("#4ADE80"))
        : new SolidColorBrush(Color.Parse("#8B91A0"));

    [RelayCommand]
    private Task Edit() => _onEdit(Profile);

    [RelayCommand]
    private Task Deactivate() => _onDeactivate(Profile);

    [RelayCommand]
    private Task Reactivate() => _onReactivate(Profile);
}
