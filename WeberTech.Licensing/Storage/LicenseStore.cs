using System.Globalization;
using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Exceptions;

namespace WeberTech.Licensing.Storage;

/// <summary>
/// Lê/grava o envelope <see cref="LicenseFile"/> no disco local do cliente,
/// e guarda um carimbo da última verificação bem-sucedida ao lado do
/// ficheiro — mitigação contra "recuar o relógio do sistema para renovar
/// um trial" (Secção 9 do roteiro): se a hora atual for anterior a esse
/// carimbo, assume-se adulteração.
/// </summary>
public sealed class LicenseStore
{
    private static readonly JsonSerializerOptions EnvelopeJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Caminho padrão do <c>.wta</c> para um dado produto — fora da pasta de
    /// instalação, para sobreviver a reinstalações/atualizações (Secção 12
    /// do roteiro): <c>%ProgramData%\WeberTech\{productId}\license.wta</c>.
    /// </summary>
    public static string GetDefaultPath(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId não pode ser vazio.", nameof(productId));

        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return Path.Combine(commonAppData, "WeberTech", productId, "license.wta");
    }

    /// <summary>Grava o envelope em <paramref name="path"/>, criando a pasta se necessário.</summary>
    public void Save(LicenseFile envelope, string path)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Caminho não pode ser vazio.", nameof(path));

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(envelope, EnvelopeJsonOptions);
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    /// <summary>
    /// Lê o envelope de <paramref name="path"/>. Devolve <c>null</c> se o
    /// ficheiro não existir (caso normal — ainda não ativado), tratado como
    /// <see cref="Enums.LicenseStatus.NotFound"/> pelo <see cref="Services.LicenseValidator"/>.
    /// </summary>
    /// <exception cref="LicenseFileCorruptedException">
    /// O ficheiro existe mas o conteúdo não é um envelope JSON válido.
    /// </exception>
    public LicenseFile? Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Caminho não pode ser vazio.", nameof(path));

        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path, Encoding.UTF8);

        try
        {
            return JsonSerializer.Deserialize<LicenseFile>(json, EnvelopeJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new LicenseFileCorruptedException(
                $"O ficheiro de licença em '{path}' existe mas não é um envelope .wta válido. " +
                "Pode ter sido editado manualmente ou truncado ao copiar.", ex);
        }
    }

    /// <summary>
    /// Regista o instante (UTC) da última verificação bem-sucedida, num
    /// ficheiro auxiliar ao lado do <c>.wta</c> (mitigação de recuo de
    /// relógio — Secção 9 do roteiro). Chamar depois de
    /// <see cref="Services.LicenseValidator.Validate"/> devolver
    /// <see cref="Enums.LicenseStatus.Valid"/>.
    /// </summary>
    public void RecordSuccessfulVerification(string licensePath, DateTime verifiedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(licensePath))
            throw new ArgumentException("Caminho não pode ser vazio.", nameof(licensePath));

        File.WriteAllText(GetVerificationStampPath(licensePath), verifiedAtUtc.ToString("O", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Lê o carimbo gravado por <see cref="RecordSuccessfulVerification"/>.
    /// Devolve <c>null</c> se nunca houve uma verificação bem-sucedida
    /// registada (primeira ativação) ou se o carimbo estiver ilegível —
    /// tratado como "sem histórico", não como erro.
    /// </summary>
    public DateTime? GetLastSuccessfulVerification(string licensePath)
    {
        if (string.IsNullOrWhiteSpace(licensePath))
            throw new ArgumentException("Caminho não pode ser vazio.", nameof(licensePath));

        string stampPath = GetVerificationStampPath(licensePath);
        if (!File.Exists(stampPath))
            return null;

        string content = File.ReadAllText(stampPath).Trim();
        return DateTime.TryParse(content, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime result)
            ? result
            : null;
    }

    private static string GetVerificationStampPath(string licensePath) => licensePath + ".lastcheck";
}
