using System.Security.Cryptography;
using WeberTech.Licensing.Crypto;
using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Enums;
using WeberTech.Licensing.Services;
using WeberTech.Licensing.Tests.Fakes;
using Xunit;

namespace WeberTech.Licensing.Tests.Services;

public class LicenseValidatorTests
{
    private const string ProductId = "kivenda.desktop_v03";
    private const string MachineId = "9F2C7A1E4B6D0083";

    private readonly SignatureService _signatureService = new();
    private readonly LicenseIssuer _issuer;
    private readonly LicenseValidator _sut;

    public LicenseValidatorTests()
    {
        _issuer = new LicenseIssuer(_signatureService);
        _sut = new LicenseValidator(_signatureService);
    }

    [Fact]
    public void Validate_ComLicencaValida_DevolveValid()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();
        License license = LicenseTestData.CreateSample(ProductId, MachineId, expiresAt: DateTime.UtcNow.AddYears(1));
        LicenseFile envelope = _issuer.Issue(license, keyPair);

        LicenseValidationResult result = _sut.Validate(envelope, ProductId, MachineId, keyPair);

        Assert.Equal(LicenseStatus.Valid, result.Status);
        Assert.NotNull(result.License);
        Assert.Equal(license.LicenseId, result.License!.LicenseId);
    }

    [Fact]
    public void Validate_ComLicencaPerpetua_NuncaExpira()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();
        License license = LicenseTestData.CreateSample(ProductId, MachineId);
        license.ExpiresAt = null;
        license.Type = LicenseType.Perpetual;
        LicenseFile envelope = _issuer.Issue(license, keyPair);

        LicenseValidationResult result = _sut.Validate(envelope, ProductId, MachineId, keyPair);

        Assert.Equal(LicenseStatus.Valid, result.Status);
    }

    [Fact]
    public void Validate_SemEnvelope_DevolveNotFound()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();

        LicenseValidationResult result = _sut.Validate(null, ProductId, MachineId, keyPair);

        Assert.Equal(LicenseStatus.NotFound, result.Status);
        Assert.Null(result.License);
    }

    [Fact]
    public void Validate_ComAssinaturaAdulterada_DevolveInvalid()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();
        License license = LicenseTestData.CreateSample(ProductId, MachineId);
        LicenseFile envelope = _issuer.Issue(license, keyPair);

        // Adultera a assinatura sem tocar no payload.
        byte[] signatureBytes = Convert.FromBase64String(envelope.Signature);
        signatureBytes[0] ^= 0xFF;
        var envelopeAdulterado = new LicenseFile
        {
            Payload = envelope.Payload,
            Signature = Convert.ToBase64String(signatureBytes),
            Algorithm = envelope.Algorithm,
            KeyVersion = envelope.KeyVersion
        };

        LicenseValidationResult result = _sut.Validate(envelopeAdulterado, ProductId, MachineId, keyPair);

        Assert.Equal(LicenseStatus.Invalid, result.Status);
        Assert.Null(result.License);
    }

    [Fact]
    public void Validate_ComPayloadAdulterado_DevolveInvalid()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();
        License license = LicenseTestData.CreateSample(ProductId, MachineId);
        LicenseFile envelope = _issuer.Issue(license, keyPair);

        // Simula alguém a editar o payload à mão (ex.: para mudar expiresAt)
        // sem conseguir re-assinar — o que é exatamente o ponto da assinatura.
        byte[] payloadBytes = Convert.FromBase64String(envelope.Payload);
        payloadBytes[^1] ^= 0xFF;
        var envelopeAdulterado = new LicenseFile
        {
            Payload = Convert.ToBase64String(payloadBytes),
            Signature = envelope.Signature,
            Algorithm = envelope.Algorithm,
            KeyVersion = envelope.KeyVersion
        };

        LicenseValidationResult result = _sut.Validate(envelopeAdulterado, ProductId, MachineId, keyPair);

        Assert.Equal(LicenseStatus.Invalid, result.Status);
    }

    [Fact]
    public void Validate_ComChavePublicaDeOutroPar_DevolveInvalid()
    {
        using RSA parCorreto = LicenseTestData.CreateEphemeralKeyPair();
        using RSA parErrado = LicenseTestData.CreateEphemeralKeyPair();
        License license = LicenseTestData.CreateSample(ProductId, MachineId);
        LicenseFile envelope = _issuer.Issue(license, parCorreto);

        LicenseValidationResult result = _sut.Validate(envelope, ProductId, MachineId, parErrado);

        Assert.Equal(LicenseStatus.Invalid, result.Status);
    }

    [Fact]
    public void Validate_ComProductIdDiferente_DevolveProductMismatch()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();
        License license = LicenseTestData.CreateSample(ProductId, MachineId);
        LicenseFile envelope = _issuer.Issue(license, keyPair);

        LicenseValidationResult result = _sut.Validate(envelope, "schoolmanager.desktop_v01", MachineId, keyPair);

        Assert.Equal(LicenseStatus.ProductMismatch, result.Status);
        Assert.NotNull(result.License); // assinatura válida — dá para mostrar dados da licença mesmo assim
    }

    [Fact]
    public void Validate_ComMachineIdDiferente_DevolveMachineMismatch()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();
        License license = LicenseTestData.CreateSample(ProductId, MachineId);
        LicenseFile envelope = _issuer.Issue(license, keyPair);

        LicenseValidationResult result = _sut.Validate(envelope, ProductId, "OUTRA-MAQUINA-0000", keyPair);

        Assert.Equal(LicenseStatus.MachineMismatch, result.Status);
    }

    [Fact]
    public void Validate_ComDataDeValidadeNoPassado_DevolveExpired()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();
        License license = LicenseTestData.CreateSample(ProductId, MachineId, expiresAt: DateTime.UtcNow.AddDays(-1));
        LicenseFile envelope = _issuer.Issue(license, keyPair);

        LicenseValidationResult result = _sut.Validate(envelope, ProductId, MachineId, keyPair);

        Assert.Equal(LicenseStatus.Expired, result.Status);
    }

    [Fact]
    public void Validate_VerificaProductMismatchAntesDeMachineMismatch()
    {
        // Ordem importa para a mensagem mostrada ao utilizador (Secção 8.4
        // do roteiro segue esta ordem) — confirma que ambos errados dá
        // ProductMismatch, não MachineMismatch.
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();
        License license = LicenseTestData.CreateSample(ProductId, MachineId);
        LicenseFile envelope = _issuer.Issue(license, keyPair);

        LicenseValidationResult result = _sut.Validate(envelope, "outro-produto", "outra-maquina", keyPair);

        Assert.Equal(LicenseStatus.ProductMismatch, result.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ComProductIdVazio_LancaArgumentException(string productId)
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();

        Assert.Throws<ArgumentException>(() => _sut.Validate(null, productId, MachineId, keyPair));
    }
}
