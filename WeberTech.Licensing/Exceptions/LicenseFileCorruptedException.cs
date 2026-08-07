namespace WeberTech.Licensing.Exceptions;

/// <summary>
/// Lançada quando um ficheiro <c>.wta</c> existe em disco mas o seu
/// conteúdo não é um envelope <see cref="Entities.LicenseFile"/> JSON válido
/// (ficheiro truncado, editado à mão de forma inválida, ou de outro
/// formato). Distinta de uma assinatura inválida — isto é sobre o
/// envelope em si estar ilegível, antes mesmo de se tentar verificar
/// a assinatura.
/// </summary>
public sealed class LicenseFileCorruptedException : WeberTechLicensingException
{
    public LicenseFileCorruptedException(string message) : base(message) { }

    public LicenseFileCorruptedException(string message, Exception innerException)
        : base(message, innerException) { }
}
