using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace WeberTech.LicenseGenerator.Services;

/// <summary>
/// Decodifica um QR Code a partir de uma imagem (ficheiro carregado pelo
/// utilizador — foto, screenshot, etc.) via ZXing.Net + binding SkiaSharp.
///
/// Não faz leitura de webcam ao vivo — isso exigiria acesso a câmara
/// nativo e captura de vídeo em tempo real, multiplataforma (Linux dev /
/// Windows produção), escopo bem maior que o resto da Fase 6. O caminho
/// de carregar uma imagem cobre o cenário mais comum descrito no PDF
/// original (Secção 6): o cliente fotografa o QR com o telemóvel e envia
/// por WhatsApp — a imagem recebida é o que se carrega aqui.
/// </summary>
public sealed class QrImageDecoderService
{
    /// <summary>Devolve o texto decodificado, ou <c>null</c> se não encontrar nenhum QR Code legível na imagem.</summary>
    public string? DecodeFromFile(string imagePath)
    {
        using SKBitmap? bitmap = SKBitmap.Decode(imagePath);
        if (bitmap is null)
            return null;

        var reader = new ZXing.SkiaSharp.BarcodeReader
        {
            Options = new DecodingOptions
            {
                PossibleFormats = [BarcodeFormat.QR_CODE],
                TryHarder = true
            }
        };

        Result? result = reader.Decode(bitmap);
        return result?.Text;
    }
}
