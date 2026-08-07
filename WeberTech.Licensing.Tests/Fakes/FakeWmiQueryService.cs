using WeberTech.Licensing.Services;

namespace WeberTech.Licensing.Tests.Fakes;

/// <summary>
/// Substituto de <see cref="IWmiQueryService"/> para testes — devolve
/// valores fixos configurados no construtor, em vez de consultar WMI real
/// (que só existe no Windows e depende do hardware da máquina em execução).
/// </summary>
public sealed class FakeWmiQueryService : IWmiQueryService
{
    private readonly string _cpuId;
    private readonly string _boardId;
    private readonly string _diskId;

    public FakeWmiQueryService(string cpuId, string boardId, string diskId)
    {
        _cpuId = cpuId;
        _boardId = boardId;
        _diskId = diskId;
    }

    public string QueryFirst(string wmiClass, string propertyName) => wmiClass switch
    {
        "Win32_Processor" => _cpuId,
        "Win32_BaseBoard" => _boardId,
        "Win32_DiskDrive" => _diskId,
        _ => throw new ArgumentOutOfRangeException(nameof(wmiClass), wmiClass, "Classe WMI não coberta pelo fake.")
    };
}
