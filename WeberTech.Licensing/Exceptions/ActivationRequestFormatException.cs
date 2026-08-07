namespace WeberTech.Licensing.Exceptions;

/// <summary>
/// Lançada quando um texto de QR Code não segue o formato esperado
/// (prefixo "WTAREQ1:" ausente, Base64 inválido, ou JSON que não
/// desserializa para <see cref="Entities.ActivationRequest"/>).
/// </summary>
public sealed class ActivationRequestFormatException : WeberTechLicensingException
{
    public ActivationRequestFormatException(string message) : base(message) { }

    public ActivationRequestFormatException(string message, Exception innerException)
        : base(message, innerException) { }
}
