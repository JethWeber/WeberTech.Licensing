using System.Security.Cryptography;
using WeberTech.Licensing.Crypto;
using WeberTech.Licensing.Exceptions;
using Xunit;

namespace WeberTech.Licensing.Tests.Crypto;

public class KeyProviderTests
{
    private readonly KeyProvider _sut = new();

    [Fact]
    public void LoadPublicKey_LeORecursoEmbutido_DevolveChaveDe3072Bits()
    {
        using RSA publicKey = _sut.LoadPublicKey();

        // A chave embutida em Properties/wt_public.pem é a de DESENVOLVIMENTO
        // gerada localmente na Fase 2 — ver nota no .csproj. Validamos apenas
        // que o formato PEM é lido corretamente e que o tamanho é o esperado.
        Assert.Equal(3072, publicKey.KeySize);
    }

    [Fact]
    public void LoadPrivateKey_ComPemNaoCifrado_CarregaComSucesso()
    {
        using RSA original = RSA.Create(2048);
        string pem = original.ExportPkcs8PrivateKeyPem();
        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pem");
        File.WriteAllText(tempPath, pem);

        try
        {
            using RSA loaded = _sut.LoadPrivateKey(tempPath);
            Assert.Equal(original.KeySize, loaded.KeySize);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void LoadPrivateKey_ComPemCifradoEPasswordCorreta_CarregaComSucesso()
    {
        using RSA original = RSA.Create(2048);
        var pbeParams = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000);
        string password = "senha-de-teste-123";
        string pem = original.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParams);
        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pem");
        File.WriteAllText(tempPath, pem);

        try
        {
            using RSA loaded = _sut.LoadPrivateKey(tempPath, password);
            Assert.Equal(original.KeySize, loaded.KeySize);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void LoadPrivateKey_ComPasswordErrada_LancaKeyLoadException()
    {
        using RSA original = RSA.Create(2048);
        var pbeParams = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000);
        string pem = original.ExportEncryptedPkcs8PrivateKeyPem("senha-correta", pbeParams);
        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pem");
        File.WriteAllText(tempPath, pem);

        try
        {
            Assert.Throws<KeyLoadException>(() => _sut.LoadPrivateKey(tempPath, "senha-errada"));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void LoadPrivateKey_ComCaminhoInexistente_LancaKeyLoadException()
    {
        string caminhoInexistente = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pem");

        Assert.Throws<KeyLoadException>(() => _sut.LoadPrivateKey(caminhoInexistente));
    }
}
