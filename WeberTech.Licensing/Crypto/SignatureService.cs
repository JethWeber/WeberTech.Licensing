namespace WeberTech.Licensing.Crypto;

/// <summary>
/// Assina (só usado pelo WeberTech.LicenseGenerator, com a chave privada) e
/// verifica (usado por qualquer produto-cliente, com a chave pública)
/// payloads JSON usando RSA-SHA256.
///
/// Decisão de arquitetura (Secção 4 / 14 do roteiro): padding <b>PSS</b>
/// (RSASignaturePadding.Pss), não PKCS#1 v1.5. Esta decisão é definitiva —
/// não pode ser alterada depois de licenças reais serem emitidas, porque uma
/// assinatura gerada com um padding não verifica com o outro.
/// </summary>
public sealed class SignatureService
{
    /// <summary>
    /// Identificador de algoritmo a gravar no campo "algorithm" do envelope
    /// .wta (ver Fase 5 — LicenseFile.Algorithm), para deixar explícito nos
    /// ficheiros emitidos qual padding foi usado.
    /// </summary>
    public const string AlgorithmIdentifier = "RSA-SHA256-PSS";

    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;
    private static readonly RSASignaturePadding Padding = RSASignaturePadding.Pss;

    /// <summary>
    /// Assina o payload JSON com a chave privada. Só deve ser chamado dentro
    /// do WeberTech.LicenseGenerator (Fase 6) — nunca por um produto-cliente.
    /// </summary>
    /// <exception cref="ArgumentException">payload vazio/nulo.</exception>
    public byte[] Sign(string payloadJson, RSA privateKey)
    {
        if (string.IsNullOrEmpty(payloadJson))
            throw new ArgumentException("O payload a assinar não pode ser vazio.", nameof(payloadJson));
        ArgumentNullException.ThrowIfNull(privateKey);

        byte[] data = Encoding.UTF8.GetBytes(payloadJson);
        return privateKey.SignData(data, HashAlgorithm, Padding);
    }

    /// <summary>
    /// Verifica a assinatura do payload JSON com a chave pública. Usado pelo
    /// LicenseValidator (Fase 5) do lado do produto-cliente.
    /// Devolve <c>false</c> em qualquer situação de assinatura inválida —
    /// nunca lança exceção por assinatura incorreta, só por argumentos
    /// malformados, para que o chamador trate isso como um LicenseStatus.Invalid
    /// normal, e não como um erro de execução.
    /// </summary>
    public bool Verify(string payloadJson, byte[] signature, RSA publicKey)
    {
        if (string.IsNullOrEmpty(payloadJson) || signature is null || signature.Length == 0)
            return false;
        ArgumentNullException.ThrowIfNull(publicKey);

        byte[] data = Encoding.UTF8.GetBytes(payloadJson);

        try
        {
            return publicKey.VerifyData(data, signature, HashAlgorithm, Padding);
        }
        catch (CryptographicException)
        {
            // Assinatura com formato/tamanho incompatível com a chave (ex.: veio
            // de outro par de chaves ou foi truncada) — tratado como inválida.
            return false;
        }
    }
}
