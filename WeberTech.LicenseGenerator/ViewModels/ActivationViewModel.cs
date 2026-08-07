using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Exceptions;
using WeberTech.Licensing.Services;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// ViewModel do Passo 1 do fluxo do emissor (ver Secção 11 do roteiro):
/// colar o texto Base64 lido do QR Code de pedido de ativação e interpretá-lo
/// com <see cref="ActivationRequestService"/>. A leitura por webcam (ZXing)
/// é uma via alternativa para preencher o mesmo <see cref="QrText"/> — ainda
/// não implementada nesta primeira versão do passo.
/// </summary>
public sealed partial class ActivationViewModel : ObservableObject
{
    private readonly ActivationRequestService _activationRequestService;

    /// <summary>Disparado quando o utilizador confirma avançar com um pedido já interpretado — o Passo 2 (IssueLicenseView) liga-se a este evento.</summary>
    public event EventHandler<ActivationRequest>? RequestConfirmed;

    [ObservableProperty]
    private string _qrText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private ActivationRequest? _parsedRequest;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Construtor sem parâmetros — usado pelo designer/preview do Avalonia.</summary>
    public ActivationViewModel() : this(new ActivationRequestService()) { }

    public ActivationViewModel(ActivationRequestService activationRequestService)
    {
        _activationRequestService = activationRequestService
            ?? throw new ArgumentNullException(nameof(activationRequestService));
    }

    [RelayCommand]
    private void ParseQrText()
    {
        ErrorMessage = null;
        ParsedRequest = null;

        string texto = QrText.Trim();
        if (texto.Length == 0)
        {
            ErrorMessage = "Cola o texto do QR Code antes de interpretar.";
            return;
        }

        try
        {
            ParsedRequest = _activationRequestService.ParseQrText(texto);
        }
        catch (ActivationRequestFormatException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        if (ParsedRequest is not null)
            RequestConfirmed?.Invoke(this, ParsedRequest);
    }

    private bool CanContinue() => ParsedRequest is not null;

    [RelayCommand]
    private void Clear()
    {
        QrText = string.Empty;
        ParsedRequest = null;
        ErrorMessage = null;
    }
}
