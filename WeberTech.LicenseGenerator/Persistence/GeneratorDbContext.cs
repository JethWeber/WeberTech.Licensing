using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WeberTech.LicenseGenerator.Entities;

namespace WeberTech.LicenseGenerator.Persistence;

/// <summary>
/// Base de dados local do <c>WeberTech.LicenseGenerator</c> — dados
/// operacionais da própria ferramenta (utilizadores da Weber Tech que a
/// usam, clientes cadastrados, perfis de produto; mais tarde, no M7,
/// também o histórico de emissões). Não confundir com o <c>.wta</c>
/// gerado para o cliente final (esse é
/// <see cref="WeberTech.Licensing.Storage.LicenseStore"/>, no Core, e vive
/// no PC do <b>cliente</b>, não da Weber Tech).
/// </summary>
public sealed class GeneratorDbContext : DbContext
{
    private readonly string _connectionString;

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ProductProfile> ProductProfiles => Set<ProductProfile>();
    public DbSet<EmissionHistoryEntry> EmissionHistory => Set<EmissionHistoryEntry>();

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

        modelBuilder.Entity<Customer>(entity =>
        {
            // Não-única de propósito — duas empresas diferentes podem
            // legitimamente ter nomes parecidos/iguais; só acelera a busca.
            entity.HasIndex(c => c.Name);
        });

        // SQLite não tem tipo de coleção nativo — List<string> é gravado
        // como texto delimitado por '|' (nomes de módulo/plano não usam
        // esse caractere). ValueComparer é necessário para o EF detetar
        // mudanças dentro da lista corretamente (senão .Add()/.Remove()
        // no meio de uma tracked entity passa despercebido).
        var stringListConverter = new ValueConverter<List<string>, string>(
            v => string.Join('|', v),
            v => v.Length == 0 ? new List<string>() : v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());

        var stringListComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
            v => v.ToList());

        modelBuilder.Entity<ProductProfile>(entity =>
        {
            entity.HasIndex(p => p.ProductId).IsUnique();

            entity.Property(p => p.AvailableFeatures)
                .HasConversion(stringListConverter)
                .Metadata.SetValueComparer(stringListComparer);

            entity.Property(p => p.AvailablePlans)
                .HasConversion(stringListConverter)
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<EmissionHistoryEntry>(entity =>
        {
            entity.HasIndex(e => e.IssuedAt);
            entity.HasIndex(e => e.ProductId);
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
