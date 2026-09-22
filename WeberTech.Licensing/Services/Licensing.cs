using WeberTech.Licensing.Crypto;
using WeberTech.Licensing.Enums;
using WeberTech.Licensing.Models;
using WeberTech.Licensing.Storage;

namespace WeberTech.Licensing.Services;

/// <summary>
/// Fachada pública de licenciamento para produtos-cliente.
/// Mantém o produto cliente dependente apenas da API pública do SDK.
/// </summary>
public static class Licensing
{
    private static readonly object Sync = new();

    private static LicenseStore? _store;
    private static LicenseValidator? _validator;
    private static MachineIdService? _machineIdService;
    private static ActivationRequestService? _activationRequestService;
    private static QrCodeService? _qrCodeService;
    private static RSA? _publicKey;
    private static string? _productId;
    private static string? _licensePath;
    private static LicenseInfo? _licenseInfo;

    public static LicenseStatus CurrentStatus { get; private set; } = LicenseStatus.NotFound;

    public static void Initialize(ProductType productType, string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId não pode ser vazio.", nameof(productId));

        _ = productType; // reservado para futuras políticas por produto.

        lock (Sync)
        {
            _productId = productId;
            _store = new LicenseStore();
            _validator = new LicenseValidator(new SignatureService());
            _machineIdService = new MachineIdService(new WmiQueryService());
            _activationRequestService = new ActivationRequestService();
            _qrCodeService = new QrCodeService();
            _publicKey?.Dispose();
            _publicKey = new KeyProvider().LoadPublicKey();
            _licensePath = LicenseStore.GetDefaultPath(productId);

            ValidarLicencaLocal();
        }
    }

    public static byte[] GenerateActivationQrCode()
    {
        EnsureInitialized();

        string machineId = _machineIdService!.GetMachineId();
        string qrText = _activationRequestService!.BuildQrText(_productId!, machineId);
        return _qrCodeService!.GeneratePng(qrText);
    }

    public static LicenseStatus ImportLicenseFile(string path)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Caminho da licença não pode ser vazio.", nameof(path));

        var envelope = _store!.Load(path);
        string machineId = _machineIdService!.GetMachineId();
        var result = _validator!.Validate(envelope, _productId!, machineId, _publicKey!);

        lock (Sync)
        {
            CurrentStatus = result.Status;
            _licenseInfo = result.License is null ? null : Map(result.License);

            if (result.Status == LicenseStatus.Valid)
            {
                _store.Save(envelope!, _licensePath!);
                _store.RecordSuccessfulVerification(_licensePath!, DateTime.UtcNow);
            }

            return CurrentStatus;
        }
    }

    public static bool HasFeature(string feature)
    {
        if (CurrentStatus != LicenseStatus.Valid)
            return false;

        // MVP KiVenda: existe apenas um pacote; uma licença válida
        // concede acesso a todas as funcionalidades.
        return !string.IsNullOrWhiteSpace(feature);
    }

    public static int DaysUntilExpiration()
    {
        var expiresAt = _licenseInfo?.ExpiresAt;
        if (!expiresAt.HasValue)
            return -1;

        return Math.Max(0, (int)Math.Ceiling((expiresAt.Value - DateTime.UtcNow).TotalDays));
    }

    public static LicenseInfo? GetLicenseInfo() => _licenseInfo;

    public static string? GetLicensePath() => _licensePath;

    private static void ValidarLicencaLocal()
    {
        var envelope = _store!.Load(_licensePath!);
        string machineId = _machineIdService!.GetMachineId();
        var result = _validator!.Validate(envelope, _productId!, machineId, _publicKey!);

        CurrentStatus = result.Status;
        _licenseInfo = result.License is null ? null : Map(result.License);

        if (result.Status == LicenseStatus.Valid)
            _store.RecordSuccessfulVerification(_licensePath!, DateTime.UtcNow);
    }

    private static void EnsureInitialized()
    {
        if (_store is null || _validator is null || _machineIdService is null ||
            _activationRequestService is null || _qrCodeService is null ||
            _publicKey is null || string.IsNullOrWhiteSpace(_productId) ||
            string.IsNullOrWhiteSpace(_licensePath))
        {
            throw new InvalidOperationException(
                "WeberTech Licensing ainda não foi inicializado. Chama Licensing.Initialize(...) no arranque.");
        }
    }

    private static LicenseInfo Map(Entities.License license) =>
        new(
            license.LicenseId,
            license.ProductId,
            license.CustomerId,
            license.CustomerName,
            license.MachineId,
            license.Plan,
            license.IssuedAt,
            license.ExpiresAt);
}
