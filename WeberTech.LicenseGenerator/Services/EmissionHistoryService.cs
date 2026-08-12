using Microsoft.EntityFrameworkCore;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Persistence;

namespace WeberTech.LicenseGenerator.Services;

/// <summary>
/// Regista e lista o histórico local de licenças emitidas (M7, Fase 6).
/// </summary>
public sealed class EmissionHistoryService
{
    private readonly Func<GeneratorDbContext> _dbContextFactory;

    public EmissionHistoryService() : this(() => new GeneratorDbContext()) { }

    public EmissionHistoryService(Func<GeneratorDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    /// <summary>
    /// Chamado pelo <c>IssueLicenseViewModel</c> logo após o `.wta` ser
    /// gravado com sucesso (M6). Nunca lança — se o registo do histórico
    /// falhar (ex.: esquema desatualizado), a emissão em si já teve
    /// sucesso e não deve ser desfeita/escondida do utilizador por causa
    /// disto; a falha só fica no log de debug.
    /// </summary>
    public async Task RecordAsync(EmissionHistoryEntry entry)
    {
        await using GeneratorDbContext db = _dbContextFactory();
        db.EmissionHistory.Add(entry);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            System.Diagnostics.Debug.WriteLine(
                $"[EmissionHistoryService] Falha ao registar histórico (licença já foi emitida com sucesso): " +
                DatabaseErrorHelper.DescribeError(ex));
        }
    }

    /// <summary>
    /// Lista o histórico, mais recente primeiro. <paramref name="searchTerm"/>
    /// procura em cliente e Machine ID. Limitado a 200 resultados —
    /// suficiente por agora, sem paginação.
    /// </summary>
    public async Task<IReadOnlyList<EmissionHistoryEntry>> ListAsync(
        string? searchTerm = null,
        string? productId = null,
        DateTime? issuedFrom = null,
        DateTime? issuedTo = null)
    {
        await using GeneratorDbContext db = _dbContextFactory();
        IQueryable<EmissionHistoryEntry> query = db.EmissionHistory.OrderByDescending(e => e.IssuedAt);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim();
            query = query.Where(e =>
                EF.Functions.Like(e.CustomerName, $"%{term}%") ||
                EF.Functions.Like(e.MachineId, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(productId))
            query = query.Where(e => e.ProductId == productId);

        if (issuedFrom is not null)
            query = query.Where(e => e.IssuedAt >= issuedFrom.Value);

        if (issuedTo is not null)
            query = query.Where(e => e.IssuedAt <= issuedTo.Value);

        return await query.Take(200).ToListAsync();
    }
}
