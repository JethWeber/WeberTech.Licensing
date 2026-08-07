using WeberTech.Licensing.Crypto;
using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Enums;

namespace WeberTech.Licensing.Services;

/// <summary>
/// Valida um envelope <see cref="LicenseFile"/>: assinatura RSA, depois
/// correspondência de produto, correspondência de máquina, e expiração —
/// nesta ordem (ver Secção 8.4 do roteiro). Usado do lado do
/// produto-cliente, com a chave pública embutida.
/// </summary>
public sealed class LicenseValidator
{
    private readonly SignatureService _signatureService;

    public LicenseValidator(SignatureService signatureService)
    {
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
    }

    /// <param name="envelope">Envelope lido do disco (ver <see cref="Storage.LicenseStore.Load"/>); <c>null</c> se não houver ficheiro.</param>
    /// <param name="currentProductId">productId do produto em execução (ex.: "kivenda.desktop_v03").</param>
    /// <param name="currentMachineId">Machine ID calculado localmente (ver <see cref="MachineIdService"/>).</param>
    /// <param name="publicKey">Chave pública embutida (ver <see cref="KeyProvider.LoadPublicKey"/>).</param>
    public LicenseValidationResult Validate(
        LicenseFile? envelope,
        string currentProductId,
        string currentMachineId,
        RSA publicKey)
    {
        if (string.IsNullOrWhiteSpace(currentProductId))
            throw new ArgumentException("currentProductId não pode ser vazio.", nameof(currentProductId));
        if (string.IsNullOrWhiteSpace(currentMachineId))
            throw new ArgumentException("currentMachineId não pode ser vazio.", nameof(currentMachineId));
        ArgumentNullException.ThrowIfNull(publicKey);

        if (envelope is null)
            return new LicenseValidationResult(LicenseStatus.NotFound, License: null);

        byte[] signature;
        byte[] payloadBytes;
        try
        {
            signature = Convert.FromBase64String(envelope.Signature);
            payloadBytes = Convert.FromBase64String(envelope.Payload);
        }
        catch (FormatException)
        {
            // Envelope com Base64 malformado — tratado como assinatura
            // inválida, não como erro de sistema (ver Verify em SignatureService).
            return new LicenseValidationResult(LicenseStatus.Invalid, License: null);
        }

        string payloadJson = Encoding.UTF8.GetString(payloadBytes);

        if (!_signatureService.Verify(payloadJson, signature, publicKey))
            return new LicenseValidationResult(LicenseStatus.Invalid, License: null);

        License? license;
        try
        {
            license = JsonSerializer.Deserialize<License>(payloadJson, LicenseJsonOptions.Default);
        }
        catch (JsonException)
        {
            // Assinatura válida mas payload não é uma License reconhecível —
            // só aconteceria com uma versão de payload incompatível/futura.
            return new LicenseValidationResult(LicenseStatus.Invalid, License: null);
        }

        if (license is null)
            return new LicenseValidationResult(LicenseStatus.Invalid, License: null);

        if (!string.Equals(license.ProductId, currentProductId, StringComparison.Ordinal))
            return new LicenseValidationResult(LicenseStatus.ProductMismatch, license);

        if (!string.Equals(license.MachineId, currentMachineId, StringComparison.Ordinal))
            return new LicenseValidationResult(LicenseStatus.MachineMismatch, license);

        if (license.ExpiresAt is { } expiresAt && expiresAt < DateTime.UtcNow)
            return new LicenseValidationResult(LicenseStatus.Expired, license);

        return new LicenseValidationResult(LicenseStatus.Valid, license);
    }
}
