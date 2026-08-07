namespace WeberTech.Licensing.Services;

/// <summary>
/// Abstração sobre consultas WMI, existente só para permitir que
/// <see cref="MachineIdService"/> seja testado sem depender de hardware real
/// nem do Windows (ver Fase 3 do roteiro). Em produção, é implementada por
/// <see cref="WmiQueryService"/>; nos testes, por um fake com valores fixos.
/// </summary>
public interface IWmiQueryService
{
    /// <summary>
    /// Devolve o valor da <paramref name="propertyName"/> na primeira
    /// instância encontrada da classe WMI <paramref name="wmiClass"/>
    /// (ex.: "Win32_Processor", "ProcessorId"). Devolve string vazia se a
    /// classe existir mas a propriedade vier nula/vazia — nunca deve
    /// lançar exceção por ausência de valor, só por falha de infraestrutura.
    /// </summary>
    string QueryFirst(string wmiClass, string propertyName);
}
