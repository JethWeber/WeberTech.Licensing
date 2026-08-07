using System.Text.Json.Serialization;

namespace WeberTech.Licensing.Services;

/// <summary>
/// Opções de <see cref="JsonSerializerOptions"/> partilhadas por
/// <see cref="LicenseIssuer"/> e <see cref="LicenseValidator"/> ao
/// serializar/desserializar o payload de <see cref="Entities.License"/>, e
/// por <see cref="Storage.LicenseStore"/> ao gravar/ler o envelope
/// <see cref="Entities.LicenseFile"/>. Centralizado aqui para que emissor e
/// validador nunca divirjam na forma como interpretam o mesmo JSON — uma
/// divergência aqui invalidaria assinaturas de forma silenciosa.
/// </summary>
internal static class LicenseJsonOptions
{
    /// <summary>
    /// camelCase (compatível com os exemplos do roteiro: "licenseId",
    /// "productId", etc.) + enums serializados pelo nome (ex.: "Subscription"),
    /// não como número — necessário para o payload ser legível/auditável
    /// mesmo antes da assinatura ser verificada.
    /// </summary>
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
