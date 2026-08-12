namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>Um mês no gráfico "Emissão de Licenças" do Dashboard (M8) — altura já em pixels, pré-calculada pelo ViewModel (evita converter em XAML).</summary>
public sealed class MonthlyEmissionPoint
{
    public required string MonthLabel { get; init; }
    public required int Count { get; init; }
    public required double BarHeight { get; init; }
}
