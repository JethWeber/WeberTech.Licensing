namespace WeberTech.Licensing.Entities;

/// <summary>
/// Envelope final gravado como ficheiro <c>.wta</c> (ver Secção 8.2 do
/// roteiro). Contém o <see cref="License"/> serializado e em Base64
/// (<see cref="Payload"/>), a assinatura RSA desse payload
/// (<see cref="Signature"/>), e metadados sobre o algoritmo/versão de chave
/// usados — para permitir rotação de chaves no futuro sem quebrar o formato.
/// </summary>
public sealed class LicenseFile
{
    /// <summary><see cref="License"/> serializado em JSON e depois em Base64.</summary>
    public required string Payload { get; set; }

    /// <summary>Assinatura RSA (ver <see cref="Crypto.SignatureService"/>) do JSON de <see cref="Payload"/> antes da conversão para Base64, também em Base64.</summary>
    public required string Signature { get; set; }

    /// <summary>Identificador do algoritmo usado — ver <see cref="Crypto.SignatureService.AlgorithmIdentifier"/>.</summary>
    public string Algorithm { get; set; } = Crypto.SignatureService.AlgorithmIdentifier;

    /// <summary>Versão do par de chaves usado para assinar — permite rotação futura sem quebrar `.wta` já emitidos.</summary>
    public int KeyVersion { get; set; } = 1;
}
