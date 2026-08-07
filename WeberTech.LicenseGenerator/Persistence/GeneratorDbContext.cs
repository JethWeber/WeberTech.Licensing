using Microsoft.EntityFrameworkCore;
using WeberTech.LicenseGenerator.Entities;

namespace WeberTech.LicenseGenerator.Persistence;

/// <summary>
/// Base de dados local do <c>WeberTech.LicenseGenerator</c> — dados
/// operacionais da própria ferramenta (utilizadores da Weber Tech que a
/// usam; mais tarde, no M4/M5/M7, também clientes, perfis de produto e
/// histórico de emissões). Não confundir com o <c>.wta</c> gerado para o
/// cliente final (esse é <see cref="WeberTech.Licensing.Storage.LicenseStore"/>,
/// no Core, e vive no PC do <b>cliente</b>, não da Weber Tech).
/// </summary>
public sealed class GeneratorDbContext : DbContext
{
    private readonly string _connectionString;

    public DbSet<User> Users => Set<User>();

    public GeneratorDbContext() : this(GetDefaultConnectionString()) { }

    public GeneratorDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlite(_connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
        });
    }

    /// <summary>
    /// <c>%LocalAppData%\WeberTech\LicenseGenerator\generator.db</c> — por
    /// utilizador do Windows (cada membro da equipa da Weber Tech tem a sua
    /// própria conta de login na máquina, e a sua própria base local).
    /// </summary>
    public static string GetDefaultConnectionString()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dbDir = Path.Combine(appData, "WeberTech", "LicenseGenerator");
        Directory.CreateDirectory(dbDir);
        string dbPath = Path.Combine(dbDir, "generator.db");
        return $"Data Source={dbPath}";
    }
}
