using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.Licensing.Crypto;
using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Enums;
using WeberTech.Licensing.Exceptions;
using WeberTech.Licensing.Services;
using WeberTech.Licensing.Storage;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Services;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// Formulário completo de emissão (M6, Fase 6) — junta o que já estava
/// pronto: <see cref="ActivationRequest"/> (Fase 4/M0), <see cref="CustomerPickerViewModel"/>
/// (M4), <see cref="ProductProfilePickerViewModel"/> (M5), e o
/// <see cref="LicenseIssuer"/>/<see cref="LicenseStore"/> reais (Core,
/// Fase 5). Substitui o <c>IssueLicenseCheckpoint</c> temporário.
/// </summary>
public sealed partial class IssueLicenseViewModel : ObservableObject
{
    private readonly LicenseIssuer _licenseIssuer;
    private readonly KeyProvider _keyProvider;
    private readonly LicenseStore _licenseStore;
    private readonly FileDialogService _fileDialogService;
    private readonly EmissionHistoryService _historyService;

    public ActivationRequest Request { get; }

    public CustomerPickerViewModel CustomerPicker { get; }
    public ProductProfilePickerViewModel ProductPicker { get; }

    public ObservableCollection<LicenseType> AvailableLicenseTypes { get; } = new(Enum.GetValues<LicenseType>());
    public ObservableCollection<string> AvailablePlans { get; } = new();
    public ObservableCollection<FeatureSelection> Features { get; } = new();

    [ObservableProperty]
    private string? _selectedPlan;

    [ObservableProperty]
    private LicenseType _selectedLicenseType = LicenseType.Subscription;

    [ObservableProperty]
    private DateTimeOffset _startDate = DateTimeOffset.UtcNow.Date;

    [ObservableProperty]
    private DateTimeOffset? _endDate = DateTimeOffset.UtcNow.Date.AddYears(1);

    public bool IsPerpetual => SelectedLicenseType == LicenseType.Perpetual;

    partial void OnSelectedLicenseTypeChanged(LicenseType value)
    {
        OnPropertyChanged(nameof(IsPerpetual));
        if (value == LicenseType.Perpetual)
            EndDate = null;
        else
            EndDate ??= StartDate.AddYears(1);
    }

    [ObservableProperty]
    private string _privateKeyPath = string.Empty;

    [ObservableProperty]
    private string _privateKeyPassword = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    public IssueLicenseViewModel(ActivationRequest request, Func<TopLevel?> topLevelProvider)
        : this(
            request,
            new LicenseIssuer(new SignatureService()),
            new KeyProvider(),
            new LicenseStore(),
            new FileDialogService(topLevelProvider),
            new EmissionHistoryService())
    {
    }

    public IssueLicenseViewModel(
        ActivationRequest request,
        LicenseIssuer licenseIssuer,
        KeyProvider keyProvider,
        LicenseStore licenseStore,
        FileDialogService fileDialogService,
        EmissionHistoryService historyService)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        _licenseIssuer = licenseIssuer ?? throw new ArgumentNullException(nameof(licenseIssuer));
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        _licenseStore = licenseStore ?? throw new ArgumentNullException(nameof(licenseStore));
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));

        CustomerPicker = new CustomerPickerViewModel();
        ProductPicker = new ProductProfilePickerViewModel();

        // Pré-filtra a busca de produto pelo productId decodificado do QR —
        // não bloqueia a escolha (o picker é genérico e reutilizável), só
        // guia o operador para o produto certo. Diferença consciente do
        // mockup original, que trata o campo como imutável — aqui fica
        // como sugestão forte, não trava.
        ProductPicker.SearchText = request.ProductId;

        ProductPicker.ProductSelected += (_, product) => RebuildFromSelectedProduct(product);
    }

    private void RebuildFromSelectedProduct(ProductProfile product)
    {
        AvailablePlans.Clear();
        foreach (string plan in product.AvailablePlans)
            AvailablePlans.Add(plan);
        SelectedPlan = AvailablePlans.FirstOrDefault();

        Features.Clear();
        foreach (string feature in product.AvailableFeatures)
            Features.Add(new FeatureSelection(feature));
    }

    [RelayCommand]
    private async Task BrowsePrivateKeyAsync()
    {
        string? path = await _fileDialogService.PickPrivateKeyFileAsync();
        if (path is not null)
            PrivateKeyPath = path;
    }

    [RelayCommand]
    private async Task IssueAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (CustomerPicker.SelectedCustomer is null)
        {
            ErrorMessage = "Seleciona ou cria um cliente.";
            return;
        }

        if (ProductPicker.SelectedProduct is null)
        {
            ErrorMessage = "Seleciona ou cria um produto.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPlan))
        {
            ErrorMessage = "Seleciona um plano.";
            return;
        }

        if (SelectedLicenseType != LicenseType.Perpetual && EndDate is null)
        {
            ErrorMessage = "Indica a data de validade.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PrivateKeyPath))
        {
            ErrorMessage = "Seleciona o ficheiro da chave privada (.pem).";
            return;
        }

        string[] selectedFeatures = Features.Where(f => f.IsSelected).Select(f => f.Name).ToArray();
        if (selectedFeatures.Length == 0)
        {
            ErrorMessage = "Seleciona pelo menos um módulo.";
            return;
        }

        IsBusy = true;
        try
        {
            RSA privateKey;
            try
            {
                privateKey = _keyProvider.LoadPrivateKey(PrivateKeyPath, PrivateKeyPassword);
            }
            catch (KeyLoadException ex)
            {
                ErrorMessage = ex.Message;
                return;
            }

            using (privateKey)
            {
                var license = new License
                {
                    LicenseId = Guid.NewGuid(),
                    ProductId = ProductPicker.SelectedProduct.ProductId,
                    CustomerId = CustomerPicker.SelectedCustomer.Id.ToString(),
                    CustomerName = CustomerPicker.SelectedCustomer.Name,
                    MachineId = Request.MachineId,
                    Plan = SelectedPlan,
                    Type = SelectedLicenseType,
                    Features = selectedFeatures,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = SelectedLicenseType == LicenseType.Perpetual ? null : EndDate!.Value.UtcDateTime
                };

                LicenseFile envelope = _licenseIssuer.Issue(license, privateKey);

                string suggestedFileName = SanitizeFileName(
                    $"{CustomerPicker.SelectedCustomer.Name}_{ProductPicker.SelectedProduct.Name}") + ".wta";

                string? savePath = await _fileDialogService.PickSaveWtaFileAsync(suggestedFileName);
                if (savePath is null)
                    return; // utilizador cancelou o diálogo de gravar — não é erro

                _licenseStore.Save(envelope, savePath);

                await _historyService.RecordAsync(new EmissionHistoryEntry
                {
                    LicenseId = license.LicenseId,
                    ProductId = license.ProductId,
                    ProductName = ProductPicker.SelectedProduct.Name,
                    CustomerName = CustomerPicker.SelectedCustomer.Name,
                    Plan = license.Plan,
                    LicenseType = license.Type.ToString(),
                    MachineId = license.MachineId,
                    IssuedAt = license.IssuedAt,
                    ExpiresAt = license.ExpiresAt,
                    FilePath = savePath
                });

                SuccessMessage = $"Licença emitida com sucesso: {savePath}";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = $"Falha ao gravar o ficheiro: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidChar, '_');
        return value;
    }
}
