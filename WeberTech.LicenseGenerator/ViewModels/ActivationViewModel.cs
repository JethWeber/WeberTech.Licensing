using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Exceptions;
using WeberTech.Licensing.Services;
using WeberTech.LicenseGenerator.Services;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// ViewModel do Passo 1 do fluxo do emissor (ver Secção 11 do roteiro):
/// colar o texto Base64 lido do QR Code de pedido de ativação e interpretá-lo
/// com <see cref="ActivationRequestService"/>. A partir de agora também dá
/// para carregar uma imagem com o QR (<see cref="QrImageDecoderService"/>)
/// em vez de colar o texto à mão — leitura por webcam ao vivo continua
/// fora de escopo (ver comentário em <see cref="QrImageDecoderService"/>).
/// </summary>
public sealed partial class ActivationViewModel : ObservableObject
{
    private readonly ActivationRequestService _activationRequestService;
    private readonly FileDialogService _fileDialogService;
    private readonly QrImageDecoderService _qrImageDecoderService;

    /// <summary>Disparado quando o utilizador confirma avançar com um pedido já interpretado — o Passo 2 (IssueLicenseView) liga-se a este evento.</summary>
    public event EventHandler<ActivationRequest>? RequestConfirmed;

    [ObservableProperty]
    private string _qrText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private ActivationRequest? _parsedRequest;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isDecodingImage;

    /// <summary>Construtor sem parâmetros — usado pelo designer/preview do Avalonia. Sem TopLevel real, "Carregar imagem" não funciona neste modo.</summary>
    public ActivationViewModel() : this(new ActivationRequestService(), new FileDialogService(() => null), new QrImageDecoderService()) { }

    /// <summary>Usado pelo <c>NavigationShellViewModel</c> em produção — <paramref name="topLevelProvider"/> é o que permite abrir o diálogo de escolher ficheiro de verdade.</summary>
    public ActivationViewModel(Func<TopLevel?> topLevelProvider)
        : this(new ActivationRequestService(), new FileDialogService(topLevelProvider), new QrImageDecoderService())
    {
    }

    public ActivationViewModel(
        ActivationRequestService activationRequestService,
        FileDialogService fileDialogService,
        QrImageDecoderService qrImageDecoderService)
    {
        _activationRequestService = activationRequestService
            ?? throw new ArgumentNullException(nameof(activationRequestService));
        _fileDialogService = fileDialogService
            ?? throw new ArgumentNullException(nameof(fileDialogService));
        _qrImageDecoderService = qrImageDecoderService
            ?? throw new ArgumentNullException(nameof(qrImageDecoderService));
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

    /// <summary>Carrega uma imagem (foto/screenshot do QR), decodifica, preenche <see cref="QrText"/> e já interpreta — poupa o utilizador de um clique extra.</summary>
    [RelayCommand]
    private async Task LoadQrFromImageAsync()
    {
        ErrorMessage = null;
        ParsedRequest = null;

        string? imagePath = await _fileDialogService.PickQrImageFileAsync();
        if (imagePath is null)
            return; // utilizador cancelou o diálogo — não é erro

        IsDecodingImage = true;
        try
        {
            string? decoded = _qrImageDecoderService.DecodeFromFile(imagePath);
            if (decoded is null)
            {
                ErrorMessage = "Não encontrei nenhum QR Code legível nessa imagem. Tenta outra foto ou screenshot.";
                return;
            }

            QrText = decoded;
            ParseQrText();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = $"Falha ao ler o ficheiro de imagem: {ex.Message}";
        }
        finally
        {
            IsDecodingImage = false;
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
