using CommunityToolkit.Mvvm.ComponentModel;
using WeberTech.Licensing.Entities;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// ⚠️ TEMPORÁRIO — checkpoint de verificação dos M4+M5 (Fase 6). Combina o
/// <see cref="ActivationRequest"/> já interpretado (Fase 4/M0) com o
/// <see cref="CustomerPickerViewModel"/> (M4) e o
/// <see cref="ProductProfilePickerViewModel"/> (M5), só para provar
/// visualmente que ambos funcionam de ponta a ponta antes do formulário de
/// emissão completo (M6) existir. Os dois pickers em si sobrevivem e serão
/// reaproveitados lá — só este wrapper é descartável.
/// </summary>
public sealed partial class IssueLicenseCheckpointViewModel : ObservableObject
{
    public ActivationRequest Request { get; }

    public CustomerPickerViewModel CustomerPicker { get; }
    public ProductProfilePickerViewModel ProductPicker { get; }

    [ObservableProperty]
    private string? _customerStatusMessage;

    [ObservableProperty]
    private string? _productStatusMessage;

    public IssueLicenseCheckpointViewModel(ActivationRequest request)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));

        CustomerPicker = new CustomerPickerViewModel();
        CustomerPicker.CustomerSelected += (_, customer) =>
            CustomerStatusMessage = $"Cliente selecionado: {customer.Name}.";

        ProductPicker = new ProductProfilePickerViewModel();
        ProductPicker.ProductSelected += (_, product) =>
            ProductStatusMessage =
                $"Produto selecionado: {product.Name} " +
                $"({product.AvailableFeatures.Count} módulos, {product.AvailablePlans.Count} planos).";
    }
}
