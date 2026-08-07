using Microsoft.EntityFrameworkCore;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Persistence;

namespace WeberTech.LicenseGenerator.Services;

/// <summary>Resultado de uma operação de autenticação/registo — nunca lança por credenciais inválidas, só devolve <see cref="Success"/> = false.</summary>
public sealed record AuthResult(bool Success, User? User, string? ErrorMessage)
{
    public static AuthResult Ok(User user) => new(true, user, null);
    public static AuthResult Fail(string errorMessage) => new(false, null, errorMessage);
}

/// <summary>
/// Registo e autenticação de utilizadores da ferramenta (M2, Fase 6). Cada
/// operação abre e fecha o seu próprio <see cref="GeneratorDbContext"/> —
/// aceitável para uma app local de um único operador por vez.
/// </summary>
public sealed class AuthService
{
    private readonly Func<GeneratorDbContext> _dbContextFactory;

    public AuthService() : this(() => new GeneratorDbContext()) { }

    public AuthService(Func<GeneratorDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    /// <summary>
    /// Chamar uma vez no arranque da app. Usa <c>EnsureCreated</c> em vez de
    /// migrations formais por agora (Fase 6 ainda em desenvolvimento ativo;
    /// migrations entram quando o esquema estabilizar — ver TODO no roteiro).
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync()
    {
        await using GeneratorDbContext db = _dbContextFactory();
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>Usado no arranque para decidir Login vs. Register (bootstrap do primeiro utilizador).</summary>
    public async Task<bool> HasAnyUserAsync()
    {
        await using GeneratorDbContext db = _dbContextFactory();
        return await db.Users.AnyAsync();
    }

    public async Task<AuthResult> RegisterAsync(string username, string displayName, string password)
    {
        username = username?.Trim() ?? string.Empty;
        displayName = displayName?.Trim() ?? string.Empty;
        password ??= string.Empty;

        if (username.Length < 3)
            return AuthResult.Fail("O nome de utilizador precisa de pelo menos 3 caracteres.");
        if (displayName.Length == 0)
            return AuthResult.Fail("Indica o teu nome.");
        if (password.Length < 8)
            return AuthResult.Fail("A password precisa de pelo menos 8 caracteres.");

        await using GeneratorDbContext db = _dbContextFactory();

        bool exists = await db.Users.AnyAsync(u => u.Username == username);
        if (exists)
            return AuthResult.Fail("Já existe um utilizador com este nome.");

        var user = new User
        {
            Username = username,
            DisplayName = displayName,
            PasswordHash = PasswordHasher.Hash(password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return AuthResult.Ok(user);
    }

    public async Task<AuthResult> AuthenticateAsync(string username, string password)
    {
        username = username?.Trim() ?? string.Empty;
        password ??= string.Empty;

        await using GeneratorDbContext db = _dbContextFactory();
        User? user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);

        // Mensagem genérica de propósito — não distinguir "utilizador não
        // existe" de "password errada" (mesmo princípio da
        // CredenciaisInvalidasException no Core, Secção 6 do roteiro).
        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
            return AuthResult.Fail("Utilizador ou password incorretos.");

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return AuthResult.Ok(user);
    }
}
