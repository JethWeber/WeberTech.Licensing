using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Exceptions;
using WeberTech.Licensing.Storage;
using Xunit;

namespace WeberTech.Licensing.Tests.Storage;

public class LicenseStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LicenseStore _sut = new();

    public LicenseStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"wt-license-store-tests-{Guid.NewGuid()}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static LicenseFile CreateSampleEnvelope() => new()
    {
        Payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("""{"licenseId":"abc"}""")),
        Signature = Convert.ToBase64String([1, 2, 3, 4]),
        Algorithm = "RSA-SHA256-PSS",
        KeyVersion = 1
    };

    [Fact]
    public void Save_DepoisLoad_DevolveEnvelopeEquivalente()
    {
        string path = Path.Combine(_tempDir, "sub", "license.wta");
        LicenseFile original = CreateSampleEnvelope();

        _sut.Save(original, path);
        LicenseFile? carregado = _sut.Load(path);

        Assert.NotNull(carregado);
        Assert.Equal(original.Payload, carregado!.Payload);
        Assert.Equal(original.Signature, carregado.Signature);
        Assert.Equal(original.Algorithm, carregado.Algorithm);
        Assert.Equal(original.KeyVersion, carregado.KeyVersion);
    }

    [Fact]
    public void Save_CriaAsSubpastasNecessarias()
    {
        string path = Path.Combine(_tempDir, "nivel1", "nivel2", "license.wta");

        _sut.Save(CreateSampleEnvelope(), path);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Load_ComFicheiroInexistente_DevolveNull()
    {
        string path = Path.Combine(_tempDir, "nao-existe.wta");

        LicenseFile? result = _sut.Load(path);

        Assert.Null(result);
    }

    [Fact]
    public void Load_ComConteudoCorrompido_LancaLicenseFileCorruptedException()
    {
        string path = Path.Combine(_tempDir, "corrompido.wta");
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(path, "isto claramente não é JSON válido {{{");

        Assert.Throws<LicenseFileCorruptedException>(() => _sut.Load(path));
    }

    [Fact]
    public void RecordSuccessfulVerification_DepoisGetLastSuccessfulVerification_DevolveMesmoInstante()
    {
        string path = Path.Combine(_tempDir, "license.wta");
        Directory.CreateDirectory(_tempDir);
        var instante = new DateTime(2026, 8, 7, 12, 30, 0, DateTimeKind.Utc);

        _sut.RecordSuccessfulVerification(path, instante);
        DateTime? lido = _sut.GetLastSuccessfulVerification(path);

        Assert.Equal(instante, lido);
    }

    [Fact]
    public void GetLastSuccessfulVerification_SemVerificacaoAnterior_DevolveNull()
    {
        string path = Path.Combine(_tempDir, "nunca-verificado.wta");

        DateTime? lido = _sut.GetLastSuccessfulVerification(path);

        Assert.Null(lido);
    }

    [Fact]
    public void GetDefaultPath_TerminaComProductIdELicenseWta()
    {
        string path = LicenseStore.GetDefaultPath("kivenda.desktop_v03");

        Assert.Contains("WeberTech", path);
        Assert.Contains("kivenda.desktop_v03", path);
        Assert.EndsWith(Path.Combine("kivenda.desktop_v03", "license.wta"), path);
    }
}
