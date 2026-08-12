using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using WeberTech.Licensing.Crypto;
using WeberTech.Licensing.Exceptions;
using WeberTech.Licensing.Services;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// Bloco "Segurança" de Configurações (M9, Fase 6) — só metadados da
/// chave <b>pública</b> embutida (algoritmo, tamanho, fingerprint SHA-256).
///
/// O mockup original mostrava uma "Chave Mestra" em texto copiável — isso
/// foi rejeitado desde que o M9 entrou no plano (Secção 4 do roteiro): a
/// chave privada nunca aparece em texto nem é copiável nesta app. Aliás,
/// nem existe "guardada" aqui — é fornecida a cada emissão via formulário
/// (M6) e descartada logo a seguir, nunca persistida.
/// </summary>
public sealed partial class SecurityInfoViewModel : ObservableObject
{
    [ObservableProperty]
    private int _keySizeBits;

    [ObservableProperty]
    private string _publicKeyFingerprint = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public string Algorithm => SignatureService.AlgorithmIdentifier;

    public SecurityInfoViewModel()
    {
        LoadPublicKeyInfo();
    }

    private void LoadPublicKeyInfo()
    {
        try
        {
            using RSA publicKey = new KeyProvider().LoadPublicKey();
            KeySizeBits = publicKey.KeySize;

            byte[] subjectPublicKeyInfo = publicKey.ExportSubjectPublicKeyInfo();
            byte[] hash = SHA256.HashData(subjectPublicKeyInfo);
            PublicKeyFingerprint = string.Join(":", hash.Select(b => b.ToString("X2")));
        }
        catch (KeyLoadException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
