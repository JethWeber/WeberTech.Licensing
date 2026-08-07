using WeberTech.Licensing.Services;
using WeberTech.Licensing.Tests.Fakes;
using Xunit;

namespace WeberTech.Licensing.Tests.Services;

public class MachineIdServiceTests
{
    [Fact]
    public void GetMachineId_ComMesmoHardware_DevolveSempreOMesmoValor()
    {
        var wmi = new FakeWmiQueryService(cpuId: "CPU-123", boardId: "BOARD-456", diskId: "DISK-789");
        var sut = new MachineIdService(wmi);

        string primeiraChamada = sut.GetMachineId();
        string segundaChamada = sut.GetMachineId();

        Assert.Equal(primeiraChamada, segundaChamada);
    }

    [Fact]
    public void GetMachineId_TemExatamente32Caracteres()
    {
        var wmi = new FakeWmiQueryService("CPU-123", "BOARD-456", "DISK-789");
        var sut = new MachineIdService(wmi);

        string machineId = sut.GetMachineId();

        Assert.Equal(32, machineId.Length);
    }

    [Fact]
    public void GetMachineId_SoContemCaracteresHexadecimais()
    {
        var wmi = new FakeWmiQueryService("CPU-123", "BOARD-456", "DISK-789");
        var sut = new MachineIdService(wmi);

        string machineId = sut.GetMachineId();

        Assert.Matches("^[0-9A-F]{32}$", machineId);
    }

    [Theory]
    [InlineData("CPU-999", "BOARD-456", "DISK-789")] // CPU diferente
    [InlineData("CPU-123", "BOARD-999", "DISK-789")] // motherboard diferente (troca real de hardware)
    [InlineData("CPU-123", "BOARD-456", "DISK-999")] // disco de sistema diferente
    public void GetMachineId_ComHardwareDiferente_DevolveValorDiferente(string cpuId, string boardId, string diskId)
    {
        var wmiOriginal = new FakeWmiQueryService("CPU-123", "BOARD-456", "DISK-789");
        var wmiAlterado = new FakeWmiQueryService(cpuId, boardId, diskId);

        string idOriginal = new MachineIdService(wmiOriginal).GetMachineId();
        string idAlterado = new MachineIdService(wmiAlterado).GetMachineId();

        Assert.NotEqual(idOriginal, idAlterado);
    }

    [Fact]
    public void GetMachineId_TrocarDiscoExternoOuImpressora_NaoAfetaOId()
    {
        // O serviço só consulta Win32_DiskDrive (disco de sistema) — a troca
        // de periféricos externos não passa por nenhuma das três classes WMI
        // usadas, logo dois cálculos com os MESMOS três valores de sistema
        // têm de bater, independentemente do que mudou "por fora".
        var wmiAntes = new FakeWmiQueryService("CPU-123", "BOARD-456", "DISK-789");
        var wmiDepois = new FakeWmiQueryService("CPU-123", "BOARD-456", "DISK-789");

        string idAntes = new MachineIdService(wmiAntes).GetMachineId();
        string idDepois = new MachineIdService(wmiDepois).GetMachineId();

        Assert.Equal(idAntes, idDepois);
    }

    [Fact]
    public void Constructor_ComWmiQueryServiceNulo_LancaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MachineIdService(null!));
    }
}
