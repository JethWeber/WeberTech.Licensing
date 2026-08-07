namespace WeberTech.Licensing.Services;

/// <summary>
/// Calcula um identificador estável da máquina local, usado para prender
/// cada licença a um computador específico (ver Fase 3 e Secção 5 do
/// roteiro). Corre inteiramente no PC do cliente, sem qualquer chamada
/// externa — só consultas WMI locais.
///
/// Combina três valores que raramente mudam sem que a máquina física
/// mude: <c>Win32_Processor.ProcessorId</c>,
/// <c>Win32_BaseBoard.SerialNumber</c> e <c>Win32_DiskDrive.SerialNumber</c>
/// (disco de sistema). Trocar um disco externo ou uma impressora não afeta
/// o resultado; trocar a motherboard gera um novo Machine ID e exige nova
/// ativação — comportamento intencional, não um bug.
/// </summary>
public sealed class MachineIdService
{
    private const int MachineIdLength = 32;

    private readonly IWmiQueryService _wmiQueryService;

    public MachineIdService(IWmiQueryService wmiQueryService)
    {
        _wmiQueryService = wmiQueryService ?? throw new ArgumentNullException(nameof(wmiQueryService));
    }

    /// <summary>
    /// Devolve o Machine ID: SHA-256 de "cpuId|boardId|diskId", truncado
    /// a 32 caracteres hexadecimais (determinístico — mesmo hardware
    /// produz sempre o mesmo valor).
    /// </summary>
    public string GetMachineId()
    {
        string cpuId = _wmiQueryService.QueryFirst("Win32_Processor", "ProcessorId");
        string boardId = _wmiQueryService.QueryFirst("Win32_BaseBoard", "SerialNumber");
        string diskId = _wmiQueryService.QueryFirst("Win32_DiskDrive", "SerialNumber");

        string raw = $"{cpuId}|{boardId}|{diskId}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));

        return Convert.ToHexString(hash)[..MachineIdLength];
    }
}
