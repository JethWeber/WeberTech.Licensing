using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Services;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// Dashboard (M8, Fase 6) — tudo calculado a partir do
/// <see cref="EmissionHistoryService"/> real (M7). Nenhum número aqui é
/// fixo/mockado; se o histórico estiver vazio, os KPIs mostram zero de
/// verdade, não um valor de exemplo.
///
/// Diferença consciente do mockup original: o KPI "Receita Estimada" foi
/// substituído por "Clientes Distintos" — nada no modelo de dados (nem
/// <c>ProductProfile</c>, nem <c>License</c> do Core) guarda preço, então
/// não há como calcular receita sem inventar um número.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private const double ChartMaxBarHeight = 160;
    private const int RecentActivityCount = 5;
    private const int ChartMonths = 6;

    private static readonly string[] MonthAbbreviations =
        ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];

    private readonly EmissionHistoryService _historyService;

    [ObservableProperty]
    private int _totalLicenses;

    [ObservableProperty]
    private int _activeLicenses;

    [ObservableProperty]
    private int _expiringSoon;

    [ObservableProperty]
    private int _distinctCustomers;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<MonthlyEmissionPoint> MonthlyEmissions { get; } = new();
    public ObservableCollection<HistoryRow> RecentActivity { get; } = new();

    public bool HasNoData => !IsLoading && TotalLicenses == 0;

    public DashboardViewModel() : this(new EmissionHistoryService()) { }

    public DashboardViewModel(EmissionHistoryService historyService)
    {
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        ErrorMessage = null;
        IsLoading = true;
        try
        {
            IReadOnlyList<EmissionHistoryEntry> all = await _historyService.ListAsync();

            DateTime nowUtc = DateTime.UtcNow;

            TotalLicenses = all.Count;
            ActiveLicenses = all.Count(e => e.ExpiresAt is null || e.ExpiresAt > nowUtc);
            ExpiringSoon = all.Count(e => e.ExpiresAt is { } exp && exp > nowUtc && exp <= nowUtc.AddDays(30));
            DistinctCustomers = all
                .Select(e => e.CustomerName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            BuildMonthlyChart(all);

            RecentActivity.Clear();
            foreach (EmissionHistoryEntry entry in all.Take(RecentActivityCount))
                RecentActivity.Add(new HistoryRow(entry));
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            ErrorMessage = DatabaseErrorHelper.DescribeError(ex);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoData));
        }
    }

    /// <summary>Últimos 6 meses (incluindo o atual), contando emissões por mês — <see cref="EmissionHistoryEntry.IssuedAt"/> convertido para hora local antes de agrupar.</summary>
    private void BuildMonthlyChart(IReadOnlyList<EmissionHistoryEntry> all)
    {
        MonthlyEmissions.Clear();

        DateTime todayLocal = DateTime.Now;
        var firstOfCurrentMonth = new DateTime(todayLocal.Year, todayLocal.Month, 1);

        List<DateTime> months = Enumerable.Range(0, ChartMonths)
            .Select(i => firstOfCurrentMonth.AddMonths(-(ChartMonths - 1 - i)))
            .ToList();

        List<int> counts = months
            .Select(month => all.Count(e =>
            {
                DateTime issuedLocal = e.IssuedAt.ToLocalTime();
                return issuedLocal.Year == month.Year && issuedLocal.Month == month.Month;
            }))
            .ToList();

        int max = Math.Max(1, counts.Max());

        for (int i = 0; i < months.Count; i++)
        {
            double barHeight = counts[i] == 0 ? 4 : Math.Max(8, ChartMaxBarHeight * counts[i] / max);
            MonthlyEmissions.Add(new MonthlyEmissionPoint
            {
                MonthLabel = MonthAbbreviations[months[i].Month - 1],
                Count = counts[i],
                BarHeight = barHeight
            });
        }
    }
}
