using System.Security.Cryptography;
using WeberTech.Licensing.Crypto;
using Xunit;

namespace WeberTech.Licensing.Tests.Crypto;

public class SignatureServiceTests
{
    private readonly SignatureService _sut = new();

    // Chaves efémeras, geradas em memória só para este teste — nunca tocam
    // em disco e nunca são a chave real da Weber Tech (ver Fase 2 do roteiro).
    private static RSA CreateEphemeralKeyPair() => RSA.Create(3072);

    [Fact]
    public void Verify_ComAssinaturaValida_DevolveTrue()
    {
        using RSA keyPair = CreateEphemeralKeyPair();
        const string payload = """{"licenseId":"abc-123","productId":"kivenda.desktop_v03"}""";

        byte[] signature = _sut.Sign(payload, keyPair);
        bool result = _sut.Verify(payload, signature, keyPair);

        Assert.True(result);
    }

    [Fact]
    public void Verify_ComPayloadAlteradoAposAssinar_DevolveFalse()
    {
        using RSA keyPair = CreateEphemeralKeyPair();
        const string payloadOriginal = """{"licenseId":"abc-123","expiresAt":"2027-01-01"}""";
        const string payloadAdulterado = """{"licenseId":"abc-123","expiresAt":"2099-01-01"}""";

        byte[] signature = _sut.Sign(payloadOriginal, keyPair);
        bool result = _sut.Verify(payloadAdulterado, signature, keyPair);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ComChavePublicaDeOutroPar_DevolveFalse()
    {
        using RSA parCorreto = CreateEphemeralKeyPair();
        using RSA parErrado = CreateEphemeralKeyPair();
        const string payload = """{"licenseId":"abc-123"}""";

        byte[] signature = _sut.Sign(payload, parCorreto);
        bool result = _sut.Verify(payload, signature, parErrado);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ComAssinaturaTruncada_DevolveFalseSemLancarExcecao()
    {
        using RSA keyPair = CreateEphemeralKeyPair();
        const string payload = """{"licenseId":"abc-123"}""";

        byte[] signature = _sut.Sign(payload, keyPair);
        byte[] signatureTruncada = signature[..^10];

        bool result = _sut.Verify(payload, signatureTruncada, keyPair);

        Assert.False(result);
    }

    [Fact]
    public void Sign_ComPayloadVazio_LancaArgumentException()
    {
        using RSA keyPair = CreateEphemeralKeyPair();

        Assert.Throws<ArgumentException>(() => _sut.Sign(string.Empty, keyPair));
    }

    [Fact]
    public void Verify_UsaPaddingPss_AssinaturaPkcs1NaoVerifica()
    {
        // Confirma a decisão de arquitetura fixada: o serviço usa PSS.
        // Uma assinatura gerada manualmente com PKCS#1 v1.5 sobre o mesmo
        // payload e a mesma chave NÃO deve verificar através do SignatureService.
        using RSA keyPair = CreateEphemeralKeyPair();
        const string payload = """{"licenseId":"abc-123"}""";
        byte[] data = System.Text.Encoding.UTF8.GetBytes(payload);

        byte[] signaturePkcs1 = keyPair.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        bool result = _sut.Verify(payload, signaturePkcs1, keyPair);

        Assert.False(result);
    }
}
