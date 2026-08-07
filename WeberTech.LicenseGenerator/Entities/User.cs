namespace WeberTech.LicenseGenerator.Entities;

/// <summary>
/// Utilizador da própria ferramenta do emissor (staff da Weber Tech que usa
/// o LicenseGenerator) — não confundir com <c>Entities.Customer</c> (M4,
/// cliente para quem uma licença é emitida). Persistido localmente via
/// <see cref="Persistence.GeneratorDbContext"/>.
/// </summary>
public sealed class User
{
    public int Id { get; set; }

    /// <summary>Usado para login — único (índice, ver <see cref="Persistence.GeneratorDbContext"/>).</summary>
    public required string Username { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>Nunca a password em claro — ver <see cref="Services.PasswordHasher"/>.</summary>
    public required string PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}
