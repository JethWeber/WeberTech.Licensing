using WeberTech.Licensing.Crypto;
using WeberTech.Licensing.Entities;

namespace WeberTech.Licensing.Services;

/// <summary>
/// Serializa e assina uma <see cref="License"/>, produzindo o envelope
/// <see cref="LicenseFile"/> pronto a gravar como <c>.wta</c>.
///
/// <b>Uso exclusivo do <c>WeberTech.LicenseGenerator</c></b> (ver Secção 0.3
/// do roteiro) — é o único componente que tem acesso à chave privada. Nenhum
/// produto-cliente deve alguma vez instanciar/chamar esta classe com uma
/// chave real.
/// </summary>
public sealed class LicenseIssuer
{
    private readonly SignatureService _signatureService;

    public LicenseIssuer(SignatureService signatureService)
    {
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
    }

    /// <summary>
    /// Assina <paramref name="license"/> com <paramref name="privateKey"/> e
    /// devolve o envelope pronto a gravar (ver <see cref="Storage.LicenseStore.Save"/>).
    /// </summary>
    public LicenseFile Issue(License license, RSA privateKey)
    {
        ArgumentNullException.ThrowIfNull(license);
        ArgumentNullException.ThrowIfNull(privateKey);

        string payloadJson = JsonSerializer.Serialize(license, LicenseJsonOptions.Default);
        byte[] signature = _signatureService.Sign(payloadJson, privateKey);

        return new LicenseFile
        {
            Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson)),
            Signature = Convert.ToBase64String(signature),
            Algorithm = SignatureService.AlgorithmIdentifier,
            KeyVersion = 1
        };
    }
}
