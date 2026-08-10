using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Services;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// Picker de cliente — busca-enquanto-digita numa lista existente, com
/// "+ Novo cliente" inline para criar sem sair da tela (M4), e "Editar"/
/// "Desativar" para o cliente selecionado (adiantado antes do M9, a
/// pedido — a tela de gestão completa continua a ser o M9; isto aqui só
/// cobre o essencial para não bloquear testes). Componente reutilizável de
/// verdade: usado como checkpoint de verificação agora, e será a peça de
/// escolha de cliente dentro do formulário de emissão real (M6).
/// </summary>
public sealed partial class CustomerPickerViewModel : ObservableObject
{
    private readonly CustomerService _customerService;
    private int? _editingCustomerId;

    /// <summary>Disparado sempre que um cliente fica selecionado — seja escolhido da lista, seja recém-criado/editado.</summary>
    public event EventHandler<Customer>? CustomerSelected;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<Customer> Results { get; } = new();

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private bool _isAddingNewCustomer;

    [ObservableProperty]
    private bool _isEditingCustomer;

    /// <summary>Lista e botões de ação só aparecem fora dos painéis de criar/editar.</summary>
    public bool IsBrowsing => !IsAddingNewCustomer && !IsEditingCustomer;

    partial void OnIsAddingNewCustomerChanged(bool value) => OnPropertyChanged(nameof(IsBrowsing));
    partial void OnIsEditingCustomerChanged(bool value) => OnPropertyChanged(nameof(IsBrowsing));

    [ObservableProperty]
    private string _newCustomerName = string.Empty;

    [ObservableProperty]
    private string _newCustomerTaxId = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    public CustomerPickerViewModel() : this(new CustomerService()) { }

    public CustomerPickerViewModel(CustomerService customerService)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        EditSelectedCustomerCommand.NotifyCanExecuteChanged();
        DeactivateSelectedCustomerCommand.NotifyCanExecuteChanged();

        if (value is not null)
            CustomerSelected?.Invoke(this, value);
    }

    private async Task LoadAsync()
    {
        try
        {
            IReadOnlyList<Customer> customers = await _customerService.ListAsync(SearchText);
            Results.Clear();
            foreach (Customer customer in customers)
                Results.Add(customer);
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            ErrorMessage = DatabaseErrorHelper.DescribeError(ex);
        }
    }

    [RelayCommand]
    private void ShowAddNewCustomer()
    {
        ErrorMessage = null;
        StatusMessage = null;
        IsAddingNewCustomer = true;
        NewCustomerName = SearchText; // conveniência: já preenche com o que foi digitado na busca
    }

    [RelayCommand]
    private void CancelAddNewCustomer()
    {
        IsAddingNewCustomer = false;
        NewCustomerName = string.Empty;
        NewCustomerTaxId = string.Empty;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task ConfirmAddNewCustomerAsync()
    {
        ErrorMessage = null;

        CustomerResult result = await _customerService.CreateAsync(NewCustomerName, NewCustomerTaxId);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        IsAddingNewCustomer = false;
        NewCustomerName = string.Empty;
        NewCustomerTaxId = string.Empty;

        await LoadAsync();
        SelectedCustomer = result.Customer; // dispara CustomerSelected via OnSelectedCustomerChanged
    }

    private bool HasSelectedCustomer() => SelectedCustomer is not null;

    [RelayCommand(CanExecute = nameof(HasSelectedCustomer))]
    private void EditSelectedCustomer()
    {
        if (SelectedCustomer is null)
            return;

        ErrorMessage = null;
        StatusMessage = null;
        _editingCustomerId = SelectedCustomer.Id;
        NewCustomerName = SelectedCustomer.Name;
        NewCustomerTaxId = SelectedCustomer.TaxId ?? string.Empty;
        IsEditingCustomer = true;
    }

    [RelayCommand]
    private void CancelEditCustomer()
    {
        IsEditingCustomer = false;
        _editingCustomerId = null;
        NewCustomerName = string.Empty;
        NewCustomerTaxId = string.Empty;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task ConfirmEditCustomerAsync()
    {
        if (_editingCustomerId is not { } id)
            return;

        ErrorMessage = null;

        CustomerResult result = await _customerService.UpdateAsync(id, NewCustomerName, NewCustomerTaxId);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        IsEditingCustomer = false;
        _editingCustomerId = null;
        NewCustomerName = string.Empty;
        NewCustomerTaxId = string.Empty;
        StatusMessage = $"Cliente '{result.Customer!.Name}' atualizado.";

        await LoadAsync();
        SelectedCustomer = result.Customer;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedCustomer))]
    private async Task DeactivateSelectedCustomerAsync()
    {
        if (SelectedCustomer is null)
            return;

        ErrorMessage = null;
        string nomeDesativado = SelectedCustomer.Name;

        CustomerResult result = await _customerService.DeactivateAsync(SelectedCustomer.Id);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        StatusMessage = $"Cliente '{nomeDesativado}' desativado — não aparece mais na busca.";
        SelectedCustomer = null;

        await LoadAsync();
    }
}
