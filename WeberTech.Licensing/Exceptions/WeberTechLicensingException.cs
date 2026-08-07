namespace WeberTech.Licensing.Exceptions;

/// <summary>
/// Classe base de todas as exceções lançadas por WeberTech.Licensing.
/// Produtos-cliente podem apanhar esta exceção genericamente, ou os
/// tipos concretos abaixo para tratamento específico.
/// </summary>
public abstract class WeberTechLicensingException : Exception
{
    protected WeberTechLicensingException(string message) : base(message) { }

    protected WeberTechLicensingException(string message, Exception innerException)
        : base(message, innerException) { }
}
