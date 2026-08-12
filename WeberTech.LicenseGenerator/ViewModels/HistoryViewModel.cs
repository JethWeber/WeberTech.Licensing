using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Services;

namespace WeberTech.LicenseGenerator.ViewModels;

/// <summary>
/// Histórico de Licenças (M7, Fase 6) — busca por cliente/Machine ID.
/// Filtro por produto/status ainda não implementado (mockup mostra os
/// dois) — deixado para uma iteração seguinte; busca + ordenação por data
/// já cobre o essencial do "pronto quando" deste marco.
/// </summary>
public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly EmissionHistoryService _historyService;

    public ObservableCollection<HistoryRow> Entries { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLoading;

    public bool HasNoEntries => !IsLoading && Entries.Count == 0;

    public HistoryViewModel() : this(new EmissionHistoryService()) { }

    public HistoryViewModel(EmissionHistoryService historyService)
    {
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        ErrorMessage = null;
        IsLoading = true;
        try
        {
            IReadOnlyList<EmissionHistoryEntry> entries = await _historyService.ListAsync(SearchText);
            Entries.Clear();
            foreach (EmissionHistoryEntry entry in entries)
                Entries.Add(new HistoryRow(entry));
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            ErrorMessage = DatabaseErrorHelper.DescribeError(ex);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoEntries));
        }
    }
}
