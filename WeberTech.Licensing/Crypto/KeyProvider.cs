using System.Reflection;
using WeberTech.Licensing.Exceptions;

namespace WeberTech.Licensing.Crypto;

/// <summary>
/// Ponto único de acesso às chaves RSA.
///
/// <see cref="LoadPublicKey"/> é chamado por qualquer produto-cliente
/// (via <c>Licensing.Initialize</c>, Fase 7) — lê a chave pública embutida
/// como recurso no próprio assembly, nunca toca em disco.
///
/// <see cref="LoadPrivateKey"/> só deve ser chamado dentro do
/// WeberTech.LicenseGenerator (Fase 6). Nenhum produto-cliente possui,
/// nem deve alguma vez tentar carregar, a chave privada.
/// </summary>
public sealed class KeyProvider
{
    private const string PublicKeyResourceName = "wt_public.pem";

    /// <summary>
    /// Carrega a chave pública embutida como <see cref="EmbeddedResource"/>
    /// no assembly do Core (ver WeberTech.Licensing.csproj, LogicalName
    /// "wt_public.pem"). Devolve sempre uma nova instância de <see cref="RSA"/>
    /// — o chamador é responsável por libertá-la (using/Dispose).
    /// </summary>
    /// <exception cref="KeyLoadException">
    /// A chave pública não foi embutida no build, ou o conteúdo não é um PEM
    /// SubjectPublicKeyInfo válido.
    /// </exception>
    public RSA LoadPublicKey()
    {
        Assembly assembly = typeof(KeyProvider).Assembly;

        using Stream? stream = assembly.GetManifestResourceStream(PublicKeyResourceName);
        if (stream is null)
        {
            throw new KeyLoadException(
                $"Chave pública embutida '{PublicKeyResourceName}' não encontrada no assembly " +
                $"'{assembly.GetName().Name}'. Confirma que Properties/wt_public.pem existe e que " +
                "o .csproj a referencia como EmbeddedResource (ver Fase 2 do roteiro).");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        string pem = reader.ReadToEnd();

        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
            return rsa;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException or FormatException)
        {
            rsa.Dispose();
            throw new KeyLoadException(
                $"Falha ao interpretar '{PublicKeyResourceName}' como PEM SubjectPublicKeyInfo válido.", ex);
        }
    }

    /// <summary>
    /// Carrega a chave privada a partir de um ficheiro PEM em disco.
    /// Uso exclusivo do WeberTech.LicenseGenerator.
    /// </summary>
    /// <param name="pemPath">Caminho para o ficheiro .pem da chave privada (PKCS#8).</param>
    /// <param name="password">
    /// Password de decifra, se o PEM estiver protegido (recomendado — ver
    /// Secção 4.2 do roteiro). Nulo/vazio assume PEM não cifrado.
    /// </param>
    /// <exception cref="KeyLoadException">
    /// Ficheiro não encontrado, PEM inválido, ou password incorreta.
    /// </exception>
    public RSA LoadPrivateKey(string pemPath, ReadOnlySpan<char> password = default)
    {
        if (string.IsNullOrWhiteSpace(pemPath))
            throw new ArgumentException("Caminho da chave privada não pode ser vazio.", nameof(pemPath));

        if (!File.Exists(pemPath))
        {
            throw new KeyLoadException(
                $"Ficheiro de chave privada não encontrado em '{pemPath}'. " +
                "Esta chave nunca é distribuída — só deve existir no ambiente do LicenseGenerator.");
        }

        string pem = File.ReadAllText(pemPath, Encoding.UTF8);
        var rsa = RSA.Create();

        try
        {
            if (password.IsEmpty)
            {
                rsa.ImportFromPem(pem);
            }
            else
            {
                rsa.ImportFromEncryptedPem(pem, password);
            }

            return rsa;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException or FormatException)
        {
            rsa.Dispose();
            throw new KeyLoadException(
                $"Falha ao carregar a chave privada de '{pemPath}'. " +
                "Verifica se o ficheiro está corrompido ou se a password está incorreta.", ex);
        }
    }
}
