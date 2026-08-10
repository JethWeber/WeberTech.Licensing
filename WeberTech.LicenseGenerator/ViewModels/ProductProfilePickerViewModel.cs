using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Services;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// Picker de perfil de produto — busca numa lista pequena (poucos produtos,
/// filtro local em memória, sem round-trip por letra digitada) + "+ Novo
/// produto" inline (M5), e "Editar"/"Desativar" para o produto selecionado
/// (adiantado antes do M9, a pedido). Mesmo espírito do
/// <see cref="CustomerPickerViewModel"/>: componente reutilizável de
/// verdade — o M6 reaproveita isto no formulário de emissão.
/// </summary>
public sealed partial class ProductProfilePickerViewModel : ObservableObject
{
    private readonly ProductProfileService _productProfileService;
    private int? _editingProductId;

    /// <summary>Disparado sempre que um produto fica selecionado — seja escolhido da lista, seja recém-criado/editado.</summary>
    public event EventHandler<ProductProfile>? ProductSelected;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<ProductProfile> Results { get; } = new();

    [ObservableProperty]
    private ProductProfile? _selectedProduct;

    [ObservableProperty]
    private bool _isAddingNewProduct;

    [ObservableProperty]
    private bool _isEditingProduct;

    /// <summary>Lista e botões de ação só aparecem fora dos painéis de criar/editar.</summary>
    public bool IsBrowsing => !IsAddingNewProduct && !IsEditingProduct;

    partial void OnIsAddingNewProductChanged(bool value) => OnPropertyChanged(nameof(IsBrowsing));
    partial void OnIsEditingProductChanged(bool value) => OnPropertyChanged(nameof(IsBrowsing));

    [ObservableProperty]
    private string _newProductId = string.Empty;

    [ObservableProperty]
    private string _newProductName = string.Empty;

    [ObservableProperty]
    private string _newProductFeatures = string.Empty;

    [ObservableProperty]
    private string _newProductPlans = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    public ProductProfilePickerViewModel() : this(new ProductProfileService()) { }

    public ProductProfilePickerViewModel(ProductProfileService productProfileService)
    {
        _productProfileService = productProfileService ?? throw new ArgumentNullException(nameof(productProfileService));
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    partial void OnSelectedProductChanged(ProductProfile? value)
    {
        EditSelectedProductCommand.NotifyCanExecuteChanged();
        DeactivateSelectedProductCommand.NotifyCanExecuteChanged();

        if (value is not null)
            ProductSelected?.Invoke(this, value);
    }

    private async Task LoadAsync()
    {
        try
        {
            IReadOnlyList<ProductProfile> all = await _productProfileService.ListAsync();

            IEnumerable<ProductProfile> filtered = string.IsNullOrWhiteSpace(SearchText)
                ? all
                : all.Where(p =>
                    p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.ProductId.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            Results.Clear();
            foreach (ProductProfile profile in filtered)
                Results.Add(profile);
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            ErrorMessage = DatabaseErrorHelper.DescribeError(ex);
        }
    }

    [RelayCommand]
    private void ShowAddNewProduct()
    {
        ErrorMessage = null;
        StatusMessage = null;
        IsAddingNewProduct = true;
        NewProductId = string.Empty;
        NewProductName = SearchText;
        NewProductFeatures = string.Empty;
        NewProductPlans = string.Empty;
    }

    [RelayCommand]
    private void CancelAddNewProduct()
    {
        IsAddingNewProduct = false;
        NewProductId = string.Empty;
        NewProductName = string.Empty;
        NewProductFeatures = string.Empty;
        NewProductPlans = string.Empty;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task ConfirmAddNewProductAsync()
    {
        ErrorMessage = null;

        string[] features = SplitCsv(NewProductFeatures);
        string[] plans = SplitCsv(NewProductPlans);

        ProductProfileResult result = await _productProfileService.CreateAsync(NewProductId, NewProductName, features, plans);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        IsAddingNewProduct = false;
        NewProductId = string.Empty;
        NewProductName = string.Empty;
        NewProductFeatures = string.Empty;
        NewProductPlans = string.Empty;

        await LoadAsync();
        SelectedProduct = result.Profile; // dispara ProductSelected via OnSelectedProductChanged
    }

    private bool HasSelectedProduct() => SelectedProduct is not null;

    [RelayCommand(CanExecute = nameof(HasSelectedProduct))]
    private void EditSelectedProduct()
    {
        if (SelectedProduct is null)
            return;

        ErrorMessage = null;
        StatusMessage = null;
        _editingProductId = SelectedProduct.Id;
        // ProductId não é editável de propósito — não entra no formulário de edição.
        NewProductName = SelectedProduct.Name;
        NewProductFeatures = string.Join(", ", SelectedProduct.AvailableFeatures);
        NewProductPlans = string.Join(", ", SelectedProduct.AvailablePlans);
        IsEditingProduct = true;
    }

    [RelayCommand]
    private void CancelEditProduct()
    {
        IsEditingProduct = false;
        _editingProductId = null;
        NewProductName = string.Empty;
        NewProductFeatures = string.Empty;
        NewProductPlans = string.Empty;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task ConfirmEditProductAsync()
    {
        if (_editingProductId is not { } id)
            return;

        ErrorMessage = null;

        string[] features = SplitCsv(NewProductFeatures);
        string[] plans = SplitCsv(NewProductPlans);

        ProductProfileResult result = await _productProfileService.UpdateAsync(id, NewProductName, features, plans);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        IsEditingProduct = false;
        _editingProductId = null;
        NewProductName = string.Empty;
        NewProductFeatures = string.Empty;
        NewProductPlans = string.Empty;
        StatusMessage = $"Produto '{result.Profile!.Name}' atualizado.";

        await LoadAsync();
        SelectedProduct = result.Profile;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProduct))]
    private async Task DeactivateSelectedProductAsync()
    {
        if (SelectedProduct is null)
            return;

        ErrorMessage = null;
        string nomeDesativado = SelectedProduct.Name;

        ProductProfileResult result = await _productProfileService.DeactivateAsync(SelectedProduct.Id);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        StatusMessage = $"Produto '{nomeDesativado}' desativado — não aparece mais na busca.";
        SelectedProduct = null;

        await LoadAsync();
    }

    private static string[] SplitCsv(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
