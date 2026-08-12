using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Services;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// Bloco "Perfis de Produto" de Configurações (M9, Fase 6) — gestão
/// completa de verdade sobre o <see cref="ProductProfileService"/> (M5):
/// listar (com/sem inativos), criar, editar, desativar, reativar. Os
/// métodos <c>UpdateAsync</c>/<c>DeactivateAsync</c>/<c>ReactivateAsync</c>
/// já existiam desde antes do M6 (adiantados a pedido, entre M5 e M6),
/// mas só eram usados "de passagem" dentro do picker compacto do
/// formulário de emissão — esta é a primeira tela dedicada de gestão,
/// e a primeira vez que <c>ReactivateAsync</c> é exposto em qualquer UI.
/// </summary>
public sealed partial class ProductProfileSettingsViewModel : ObservableObject
{
    private readonly ProductProfileService _service;
    private int? _editingId;

    public ObservableCollection<ProductProfileRow> Rows { get; } = new();

    [ObservableProperty]
    private bool _showInactive;

    [ObservableProperty]
    private bool _isAdding;

    [ObservableProperty]
    private bool _isEditing;

    public bool IsFormOpen => IsAdding || IsEditing;

    partial void OnIsAddingChanged(bool value) => OnPropertyChanged(nameof(IsFormOpen));
    partial void OnIsEditingChanged(bool value) => OnPropertyChanged(nameof(IsFormOpen));
    partial void OnShowInactiveChanged(bool value) => _ = LoadAsync();

    [ObservableProperty]
    private string _formProductId = string.Empty;

    [ObservableProperty]
    private string _formName = string.Empty;

    [ObservableProperty]
    private string _formFeatures = string.Empty;

    [ObservableProperty]
    private string _formPlans = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    public ProductProfileSettingsViewModel() : this(new ProductProfileService()) { }

    public ProductProfileSettingsViewModel(ProductProfileService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        ErrorMessage = null;
        try
        {
            IReadOnlyList<ProductProfile> profiles = await _service.ListAsync(activeOnly: !ShowInactive);
            Rows.Clear();
            foreach (ProductProfile profile in profiles)
                Rows.Add(new ProductProfileRow(profile, EditSelectedAsync, DeactivateSelectedAsync, ReactivateSelectedAsync));
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            ErrorMessage = DatabaseErrorHelper.DescribeError(ex);
        }
    }

    [RelayCommand]
    private void ShowAddNew()
    {
        ErrorMessage = null;
        StatusMessage = null;
        _editingId = null;
        IsEditing = false;
        IsAdding = true;
        FormProductId = string.Empty;
        FormName = string.Empty;
        FormFeatures = string.Empty;
        FormPlans = string.Empty;
    }

    private Task EditSelectedAsync(ProductProfile profile)
    {
        ErrorMessage = null;
        StatusMessage = null;
        _editingId = profile.Id;
        IsAdding = false;
        IsEditing = true;
        FormProductId = profile.ProductId; // imutável — só leitura no formulário de edição
        FormName = profile.Name;
        FormFeatures = string.Join(", ", profile.AvailableFeatures);
        FormPlans = string.Join(", ", profile.AvailablePlans);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CancelForm()
    {
        IsAdding = false;
        IsEditing = false;
        _editingId = null;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task ConfirmFormAsync()
    {
        ErrorMessage = null;

        string[] features = SplitCsv(FormFeatures);
        string[] plans = SplitCsv(FormPlans);

        ProductProfileResult result = IsEditing && _editingId is { } id
            ? await _service.UpdateAsync(id, FormName, features, plans)
            : await _service.CreateAsync(FormProductId, FormName, features, plans);

        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        StatusMessage = IsEditing
            ? $"Produto '{result.Profile!.Name}' atualizado."
            : $"Produto '{result.Profile!.Name}' criado.";

        IsAdding = false;
        IsEditing = false;
        _editingId = null;

        await LoadAsync();
    }

    private async Task DeactivateSelectedAsync(ProductProfile profile)
    {
        ErrorMessage = null;
        ProductProfileResult result = await _service.DeactivateAsync(profile.Id);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }
        StatusMessage = $"'{result.Profile!.Name}' desativado.";
        await LoadAsync();
    }

    private async Task ReactivateSelectedAsync(ProductProfile profile)
    {
        ErrorMessage = null;
        ProductProfileResult result = await _service.ReactivateAsync(profile.Id);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }
        StatusMessage = $"'{result.Profile!.Name}' reativado.";
        await LoadAsync();
    }

    private static string[] SplitCsv(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
