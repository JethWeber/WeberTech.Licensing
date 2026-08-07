namespace WeberTech.Licensing.Exceptions;

/// <summary>
/// Lançada quando a chave pública embutida no binário não é encontrada
/// (build mal configurado — ver WeberTech.Licensing.csproj / Properties/wt_public.pem)
/// ou quando a chave privada não pôde ser carregada (caminho inválido,
/// PEM corrompido, ou password incorreta ao decifrar). Esta última só é
/// relevante dentro do WeberTech.LicenseGenerator — nenhum produto-cliente
/// deve alguma vez tentar carregar uma chave privada.
/// </summary>
public sealed class KeyLoadException : WeberTechLicensingException
{
    public KeyLoadException(string message) : base(message) { }

    public KeyLoadException(string message, Exception innerException)
        : base(message, innerException) { }
}
