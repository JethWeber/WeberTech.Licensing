namespace WeberTech.Licensing.Exceptions;

/// <summary>
/// Lançada quando não é possível calcular o Machine ID local — por exemplo,
/// consulta WMI a falhar, ou execução fora do Windows (o
/// <see cref="Services.WmiQueryService"/> concreto depende de WMI e só
/// funciona nesse sistema operativo; ver Fase 3 do roteiro).
/// </summary>
public sealed class MachineIdentificationException : WeberTechLicensingException
{
    public MachineIdentificationException(string message) : base(message) { }

    public MachineIdentificationException(string message, Exception innerException)
        : base(message, innerException) { }
}
