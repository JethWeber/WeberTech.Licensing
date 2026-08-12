using Avalonia.Media;
using WeberTech.LicenseGenerator.Entities;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// Linha de apresentação da tela de Histórico (M7) — embrulha
/// <see cref="EmissionHistoryEntry"/> e calcula rótulos/cores de status.
/// Cores diretas em vez de <c>Classes</c> dinâmicas em XAML — mais simples
/// de revisar sem correr o Avalonia Previewer.
/// </summary>
public sealed class HistoryRow
{
    public HistoryRow(EmissionHistoryEntry entry)
    {
        Entry = entry;
    }

    public EmissionHistoryEntry Entry { get; }

    public string IssuedAtLabel => Entry.IssuedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public string ExpiresAtLabel => Entry.ExpiresAt is { } expiresAt
        ? expiresAt.ToLocalTime().ToString("dd/MM/yyyy")
        : "Perpétua";

    public string ProductPlanLabel => $"{Entry.ProductName} · {Entry.Plan}";

    public string StatusLabel => Entry.ExpiresAt switch
    {
        null => "PERPÉTUA",
        { } exp when exp < DateTime.UtcNow => "EXPIRADA",
        { } exp when exp <= DateTime.UtcNow.AddDays(30) => "EXPIRA EM BREVE",
        _ => "ATIVA"
    };

    public IBrush StatusBackgroundBrush => Entry.ExpiresAt switch
    {
        null => new SolidColorBrush(Color.Parse("#1A8B91A0")),
        { } exp when exp < DateTime.UtcNow => new SolidColorBrush(Color.Parse("#4D93000A")),
        { } exp when exp <= DateTime.UtcNow.AddDays(30) => new SolidColorBrush(Color.Parse("#4DFBBF24")),
        _ => new SolidColorBrush(Color.Parse("#4D2E7D32"))
    };

    public IBrush StatusForegroundBrush => Entry.ExpiresAt switch
    {
        null => new SolidColorBrush(Color.Parse("#8B91A0")),
        { } exp when exp < DateTime.UtcNow => new SolidColorBrush(Color.Parse("#FFB4AB")),
        { } exp when exp <= DateTime.UtcNow.AddDays(30) => new SolidColorBrush(Color.Parse("#FBBF24")),
        _ => new SolidColorBrush(Color.Parse("#4ADE80"))
    };
}
