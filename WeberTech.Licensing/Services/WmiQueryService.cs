using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using WeberTech.Licensing.Exceptions;

namespace WeberTech.Licensing.Services;

/// <summary>
/// Implementação real de <see cref="IWmiQueryService"/>, usando
/// <c>System.Management</c>. Só funciona no Windows — é o sistema
/// operativo de todos os produtos-cliente desta arquitetura (School
/// Manager, KiVenda, SmartGest correm em desktops Windows dos clientes).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WmiQueryService : IWmiQueryService
{
    public string QueryFirst(string wmiClass, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(wmiClass))
            throw new ArgumentException("Classe WMI não pode ser vazia.", nameof(wmiClass));
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Propriedade WMI não pode ser vazia.", nameof(propertyName));

        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {wmiClass}");
            using ManagementObjectCollection results = searcher.Get();

            foreach (ManagementBaseObject item in results)
            {
                using (item)
                {
                    object? value = item[propertyName];
                    if (value is not null)
                        return value.ToString() ?? string.Empty;
                }
            }

            return string.Empty;
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or COMException)
        {
            throw new MachineIdentificationException(
                $"Falha ao consultar WMI ({wmiClass}.{propertyName}). " +
                "Confirma que o processo tem permissões suficientes e que o serviço WMI está ativo.", ex);
        }
    }
}
