using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WeberTech.LicenseGenerator.Persistence;

namespace WeberTech.LicenseGenerator.Services;

/// <summary>
/// Converte falhas de infraestrutura da base de dados local (esquema
/// desatualizado — "no such table"/"no such column", ver README "Solução
/// de problemas conhecidos") numa mensagem amigável, em vez de deixar uma
/// <see cref="DbUpdateException"/>/<see cref="SqliteException"/> crua
/// derrubar a app inteira. Usado por todo <c>*Service</c> que persiste no
/// <see cref="GeneratorDbContext"/> (Auth, Customer, ProductProfile).
/// </summary>
internal static class DatabaseErrorHelper
{
    /// <summary>
    /// Verdadeiro se <paramref name="ex"/> for (ou envolver) uma
    /// <see cref="SqliteException"/> — o chamador normalmente usa isto
    /// como filtro de <c>catch (Exception ex) when (...)</c>.
    /// </summary>
    public static bool IsDatabaseError(Exception ex) =>
        ex is SqliteException or DbUpdateException { InnerException: SqliteException };

    public static string DescribeError(Exception ex)
    {
        string? sqliteMessage = ex switch
        {
            SqliteException sqlite => sqlite.Message,
            { InnerException: SqliteException inner } => inner.Message,
            _ => null
        };

        bool pareceEsquemaDesatualizado = sqliteMessage is not null &&
            (sqliteMessage.Contains("no such table", StringComparison.OrdinalIgnoreCase) ||
             sqliteMessage.Contains("no such column", StringComparison.OrdinalIgnoreCase));

        if (pareceEsquemaDesatualizado)
        {
            string dbPath = GeneratorDbContext.GetDefaultConnectionString()
                .Replace("Data Source=", string.Empty, StringComparison.Ordinal);

            return "A base de dados local está com um esquema desatualizado " +
                   "(comum durante o desenvolvimento ativo desta fase). Fecha a app, apaga o ficheiro " +
                   $"'{dbPath}' e abre de novo — vais perder só dados de teste locais.";
        }

        return "Falha inesperada ao aceder à base de dados local. Tenta novamente.";
    }
}
