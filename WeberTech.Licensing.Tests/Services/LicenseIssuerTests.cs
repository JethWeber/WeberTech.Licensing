using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WeberTech.Licensing.Crypto;
using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Enums;
using WeberTech.Licensing.Services;
using WeberTech.Licensing.Tests.Fakes;
using Xunit;

namespace WeberTech.Licensing.Tests.Services;

public class LicenseIssuerTests
{
    private readonly LicenseIssuer _sut = new(new SignatureService());

    [Fact]
    public void Issue_DevolveEnvelopeComPayloadEAssinaturaEmBase64()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();
        License license = LicenseTestData.CreateSample();

        LicenseFile envelope = _sut.Issue(license, keyPair);

        Assert.False(string.IsNullOrEmpty(envelope.Payload));
        Assert.False(string.IsNullOrEmpty(envelope.Signature));
        Assert.Equal(SignatureService.AlgorithmIdentifier, envelope.Algorithm);
        Assert.Equal(1, envelope.KeyVersion);

        // Confirma que ambos os campos são Base64 válido, sem lançar.
        Convert.FromBase64String(envelope.Payload);
        Convert.FromBase64String(envelope.Signature);
    }

    [Fact]
    public void Issue_EmitirERelerManualmente_PreservaTodosOsCampos()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();
        License original = LicenseTestData.CreateSample();

        LicenseFile envelope = _sut.Issue(original, keyPair);

        // "Reler" aqui é deliberadamente manual (sem passar pelo
        // LicenseValidator, que tem os seus próprios testes) — isto isola
        // o teste do Issuer ao formato exato que ele produz.
        byte[] payloadBytes = Convert.FromBase64String(envelope.Payload);
        string payloadJson = Encoding.UTF8.GetString(payloadBytes);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        License? relido = JsonSerializer.Deserialize<License>(payloadJson, options);

        Assert.NotNull(relido);
        Assert.Equal(original.LicenseId, relido!.LicenseId);
        Assert.Equal(original.ProductId, relido.ProductId);
        Assert.Equal(original.CustomerId, relido.CustomerId);
        Assert.Equal(original.CustomerName, relido.CustomerName);
        Assert.Equal(original.MachineId, relido.MachineId);
        Assert.Equal(original.Plan, relido.Plan);
        Assert.Equal(original.Type, relido.Type);
        Assert.Equal(original.Features, relido.Features);
        Assert.Equal(original.IssuedAt, relido.IssuedAt);
        Assert.Equal(original.ExpiresAt, relido.ExpiresAt);
    }

    [Fact]
    public void Issue_LicencaPerpetua_ExpiresAtNuloPreservado()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();
        License perpetua = LicenseTestData.CreateSample();
        perpetua.ExpiresAt = null;
        perpetua.Type = LicenseType.Perpetual;

        LicenseFile envelope = _sut.Issue(perpetua, keyPair);

        string payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(envelope.Payload));
        Assert.Contains("\"expiresAt\":null", payloadJson);
    }

    [Fact]
    public void Issue_ComLicenseNula_LancaArgumentNullException()
    {
        using RSA keyPair = LicenseTestData.CreateEphemeralKeyPair();

        Assert.Throws<ArgumentNullException>(() => _sut.Issue(null!, keyPair));
    }

    [Fact]
    public void Issue_ComChaveNula_LancaArgumentNullException()
    {
        License license = LicenseTestData.CreateSample();

        Assert.Throws<ArgumentNullException>(() => _sut.Issue(license, null!));
    }
}
