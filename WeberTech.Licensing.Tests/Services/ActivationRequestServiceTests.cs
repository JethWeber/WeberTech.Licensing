using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Exceptions;
using WeberTech.Licensing.Services;
using Xunit;

namespace WeberTech.Licensing.Tests.Services;

public class ActivationRequestServiceTests
{
    private readonly ActivationRequestService _sut = new();

    [Fact]
    public void BuildQrText_ComeçaSempreComOPrefixoDeFormato()
    {
        string qrText = _sut.BuildQrText("kivenda.desktop_v03", "9F2C7A1E4B6D0083");

        Assert.StartsWith("WTAREQ1:", qrText, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildQrText_DepoisParseQrText_RoundTripPreservaOsCampos()
    {
        string qrText = _sut.BuildQrText("schoolmanager.desktop_v01", "AABBCCDDEE112233");

        ActivationRequest parsed = _sut.ParseQrText(qrText);

        Assert.Equal("WTAREQ1", parsed.Fmt);
        Assert.Equal("schoolmanager.desktop_v01", parsed.ProductId);
        Assert.Equal("AABBCCDDEE112233", parsed.MachineId);
        Assert.NotEqual(Guid.Empty, parsed.RequestId);
    }

    [Fact]
    public void BuildQrText_ComRequestExplicito_PreservaRequestIdERequestedAt()
    {
        var request = new ActivationRequest
        {
            ProductId = "smartgest.desktop_v01",
            MachineId = "1122334455667788",
            RequestId = Guid.Parse("b13e1a2a-8e3d-4a91-9f2b-6b6a9e6a2e10"),
            RequestedAt = new DateTime(2026, 7, 30, 9, 15, 0, DateTimeKind.Utc)
        };

        string qrText = _sut.BuildQrText(request);
        ActivationRequest parsed = _sut.ParseQrText(qrText);

        Assert.Equal(request.RequestId, parsed.RequestId);
        Assert.Equal(request.RequestedAt, parsed.RequestedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildQrText_ComProductIdVazio_LancaArgumentException(string productId)
    {
        Assert.Throws<ArgumentException>(() => _sut.BuildQrText(productId, "machine-id"));
    }

    [Fact]
    public void ParseQrText_SemPrefixoWTAREQ1_LancaActivationRequestFormatException()
    {
        string textoInvalido = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{}"));

        Assert.Throws<ActivationRequestFormatException>(() => _sut.ParseQrText(textoInvalido));
    }

    [Fact]
    public void ParseQrText_ComBase64Invalido_LancaActivationRequestFormatException()
    {
        string textoInvalido = "WTAREQ1:isto-nao-e-base64-valido!!!";

        Assert.Throws<ActivationRequestFormatException>(() => _sut.ParseQrText(textoInvalido));
    }

    [Fact]
    public void ParseQrText_ComJsonQueNaoEUmActivationRequest_LancaActivationRequestFormatException()
    {
        string jsonQualquer = """{"algumCampo":"algumValor"}""";
        string base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonQualquer));
        string qrText = $"WTAREQ1:{base64}";

        // Falta productId/machineId, que são "required" na entidade —
        // deve falhar de forma controlada, não com uma exceção de sistema.
        Assert.Throws<ActivationRequestFormatException>(() => _sut.ParseQrText(qrText));
    }

    [Fact]
    public void ParseQrText_ComTextoVazio_LancaActivationRequestFormatException()
    {
        Assert.Throws<ActivationRequestFormatException>(() => _sut.ParseQrText(string.Empty));
    }
}
