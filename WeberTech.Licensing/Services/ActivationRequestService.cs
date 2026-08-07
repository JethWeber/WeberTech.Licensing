using System.Text.Json;
using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Exceptions;

namespace WeberTech.Licensing.Services;

/// <summary>
/// Cria o texto do pedido de ativação a colocar no QR Code, e interpreta
/// esse mesmo texto do lado do <c>WeberTech.LicenseGenerator</c> (ver
/// Secção 7 do roteiro). Formato: <c>WTAREQ1:&lt;base64(json)&gt;</c>.
/// </summary>
public sealed class ActivationRequestService
{
    /// <summary>Prefixo que identifica a versão do formato do pedido.</summary>
    public const string FormatTag = "WTAREQ1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Monta o pedido de ativação e devolve o texto pronto a converter em
    /// imagem de QR Code (ver <see cref="QrCodeService"/>).
    /// </summary>
    public string BuildQrText(string productId, string machineId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId não pode ser vazio.", nameof(productId));
        if (string.IsNullOrWhiteSpace(machineId))
            throw new ArgumentException("machineId não pode ser vazio.", nameof(machineId));

        var request = new ActivationRequest
        {
            ProductId = productId,
            MachineId = machineId
        };

        return BuildQrText(request);
    }

    /// <summary>
    /// Sobrecarga que aceita um <see cref="ActivationRequest"/> já
    /// construído — útil em testes, onde <c>RequestId</c>/<c>RequestedAt</c>
    /// precisam ser determinísticos.
    /// </summary>
    public string BuildQrText(ActivationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string json = JsonSerializer.Serialize(request, JsonOptions);
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return $"{FormatTag}:{base64}";
    }

    /// <summary>
    /// Interpreta o texto lido do QR Code (ou colado manualmente no
    /// LicenseGenerator) e devolve o <see cref="ActivationRequest"/>
    /// correspondente.
    /// </summary>
    /// <exception cref="ActivationRequestFormatException">
    /// Prefixo ausente/errado, Base64 inválido, ou JSON que não desserializa
    /// para um pedido válido.
    /// </exception>
    public ActivationRequest ParseQrText(string qrText)
    {
        if (string.IsNullOrWhiteSpace(qrText))
            throw new ActivationRequestFormatException("Texto do QR Code está vazio.");

        string prefix = $"{FormatTag}:";
        if (!qrText.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new ActivationRequestFormatException(
                $"Formato de pedido não reconhecido — esperado prefixo '{prefix}'. " +
                "Confirma se o texto foi copiado por inteiro.");
        }

        string base64 = qrText[prefix.Length..];

        byte[] jsonBytes;
        try
        {
            jsonBytes = Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            throw new ActivationRequestFormatException(
                "Conteúdo do pedido não é Base64 válido — o texto pode ter sido truncado ao copiar.", ex);
        }

        string json = Encoding.UTF8.GetString(jsonBytes);

        try
        {
            ActivationRequest? request = JsonSerializer.Deserialize<ActivationRequest>(json, JsonOptions);
            if (request is null)
                throw new ActivationRequestFormatException("Pedido de ativação desserializou como nulo.");

            return request;
        }
        catch (JsonException ex)
        {
            throw new ActivationRequestFormatException(
                "Conteúdo do pedido não é um JSON de ActivationRequest válido.", ex);
        }
    }
}
