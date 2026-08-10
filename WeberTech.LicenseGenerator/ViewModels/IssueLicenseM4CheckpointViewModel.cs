using CommunityToolkit.Mvvm.ComponentModel;
using WeberTech.Licensing.Entities;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// ⚠️ TEMPORÁRIO — checkpoint de verificação do M4 (Fase 6). Combina o
/// <see cref="ActivationRequest"/> já interpretado (Fase 4/M0) com o
/// <see cref="CustomerPickerViewModel"/> novo, só para provar visualmente
/// que o cadastro de clientes funciona de ponta a ponta. O M6 substitui
/// isto pelo formulário de emissão completo (produto, plano, módulos,
/// datas) — o <see cref="CustomerPickerViewModel"/> em si sobrevive e
/// será reaproveitado lá, só este wrapper é descartável.
/// </summary>
public sealed partial class IssueLicenseM4CheckpointViewModel : ObservableObject
{
    public ActivationRequest Request { get; }

    public CustomerPickerViewModel CustomerPicker { get; }

    [ObservableProperty]
    private string? _statusMessage;

    public IssueLicenseM4CheckpointViewModel(ActivationRequest request)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        CustomerPicker = new CustomerPickerViewModel();
        CustomerPicker.CustomerSelected += (_, customer) =>
            StatusMessage = $"Cliente selecionado: {customer.Name}. " +
                "O resto do formulário de emissão (plano, módulos, datas) chega no M6.";
    }
}
