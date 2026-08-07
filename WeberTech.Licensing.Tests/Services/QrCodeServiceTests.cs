using WeberTech.Licensing.Services;
using Xunit;

namespace WeberTech.Licensing.Tests.Services;

public class QrCodeServiceTests
{
    private readonly QrCodeService _sut = new();

    // Assinatura de ficheiro PNG (RFC 2083): sempre os mesmos 8 bytes iniciais.
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void GeneratePng_ComTextoValido_DevolvePngComAssinaturaCorreta()
    {
        byte[] png = _sut.GeneratePng("WTAREQ1:qualquer-coisa-de-exemplo");

        Assert.True(png.Length > PngSignature.Length);
        Assert.Equal(PngSignature, png[..PngSignature.Length]);
    }

    [Fact]
    public void GeneratePng_ComPixelsPerModuleMaior_ProduzImagemMaior()
    {
        const string qrText = "WTAREQ1:mesmo-texto-para-comparar-tamanhos";

        byte[] pequena = _sut.GeneratePng(qrText, pixelsPerModule: 2);
        byte[] grande = _sut.GeneratePng(qrText, pixelsPerModule: 20);

        Assert.True(grande.Length > pequena.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GeneratePng_ComTextoVazioOuNulo_LancaArgumentException(string? qrText)
    {
        Assert.Throws<ArgumentException>(() => _sut.GeneratePng(qrText!));
    }

    [Fact]
    public void GeneratePng_ComPixelsPerModuleZeroOuNegativo_LancaArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GeneratePng("WTAREQ1:x", pixelsPerModule: 0));
    }
}
