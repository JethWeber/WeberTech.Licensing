namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>Página "Configurações" (M9, Fase 6) — compõe os dois blocos do mockup: Segurança e Perfis de Produto.</summary>
public sealed class SettingsViewModel
{
    public SecurityInfoViewModel Security { get; } = new();
    public ProductProfileSettingsViewModel Products { get; } = new();
}
