using QRCoder;

namespace WeberTech.Licensing.Services;

/// <summary>
/// Gera a imagem do QR Code de pedido de ativação, a partir do texto
/// produzido por <see cref="ActivationRequestService"/>.
///
/// Devolve bytes PNG em vez de um tipo de UI (ex.: <c>Bitmap</c>) para
/// respeitar o princípio de "zero dependência de UI" do Core (Secção 0.1
/// do roteiro) — cabe ao host (Desktop/API) decidir como desenhar esses
/// bytes no ecrã.
/// </summary>
public sealed class QrCodeService
{
    private const int DefaultPixelsPerModule = 10;

    /// <summary>
    /// Gera o PNG do QR Code para o texto fornecido (tipicamente o valor
    /// devolvido por <see cref="ActivationRequestService.BuildQrText(string,string)"/>).
    /// </summary>
    /// <param name="qrText">Texto a codificar — não validado aqui; ver <see cref="ActivationRequestService"/> para o formato esperado.</param>
    /// <param name="pixelsPerModule">Tamanho de cada "módulo" do QR em pixels — controla a resolução final da imagem.</param>
    public byte[] GeneratePng(string qrText, int pixelsPerModule = DefaultPixelsPerModule)
    {
        if (string.IsNullOrEmpty(qrText))
            throw new ArgumentException("Texto do QR Code não pode ser vazio.", nameof(qrText));
        if (pixelsPerModule <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelsPerModule), "Deve ser maior que zero.");

        using QRCodeGenerator generator = new();
        // Nível de correção de erro "Q" (~25%) — margem confortável para o
        // QR sobreviver a fotografia por telemóvel em condições de luz
        // variáveis (cenário principal de uso, Secção 6 do roteiro).
        using QRCodeData qrData = generator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
        using PngByteQRCode pngQrCode = new(qrData);

        return pngQrCode.GetGraphic(pixelsPerModule);
    }
}
