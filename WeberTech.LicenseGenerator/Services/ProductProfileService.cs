using Microsoft.EntityFrameworkCore;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Persistence;

namespace WeberTech.LicenseGenerator.Services;

/// <summary>Resultado de uma operação de criação de perfil de produto — nunca lança por validação, só devolve <see cref="Success"/> = false.</summary>
public sealed record ProductProfileResult(bool Success, ProductProfile? Profile, string? ErrorMessage)
{
    public static ProductProfileResult Ok(ProductProfile profile) => new(true, profile, null);
    public static ProductProfileResult Fail(string errorMessage) => new(false, null, errorMessage);
}

/// <summary>
/// CRUD mínimo de perfis de produto (M5, Fase 6) — listar e criar.
/// Desativar existe (nunca há delete físico — licenças já emitidas com um
/// perfil continuam válidas mesmo que o perfil deixe de estar disponível
/// para novas emissões).
/// </summary>
public sealed class ProductProfileService
{
    private readonly Func<GeneratorDbContext> _dbContextFactory;

    public ProductProfileService() : this(() => new GeneratorDbContext()) { }

    public ProductProfileService(Func<GeneratorDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    public async Task<IReadOnlyList<ProductProfile>> ListAsync(bool activeOnly = true)
    {
        await using GeneratorDbContext db = _dbContextFactory();
        IQueryable<ProductProfile> query = db.ProductProfiles.OrderBy(p => p.Name);

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        return await query.ToListAsync();
    }

    public async Task<ProductProfile?> GetByProductIdAsync(string productId)
    {
        await using GeneratorDbContext db = _dbContextFactory();
        return await db.ProductProfiles.FirstOrDefaultAsync(p => p.ProductId == productId);
    }

    public async Task<ProductProfileResult> CreateAsync(string productId, string name, IEnumerable<string> features, IEnumerable<string> plans)
    {
        productId = productId?.Trim() ?? string.Empty;
        name = name?.Trim() ?? string.Empty;

        if (productId.Length < 3)
            return ProductProfileResult.Fail("O identificador do produto precisa de pelo menos 3 caracteres.");
        if (name.Length == 0)
            return ProductProfileResult.Fail("Indica o nome do produto.");

        List<string> featureList = features.Select(f => f.Trim()).Where(f => f.Length > 0).Distinct().ToList();
        List<string> planList = plans.Select(p => p.Trim()).Where(p => p.Length > 0).Distinct().ToList();

        if (featureList.Count == 0)
            return ProductProfileResult.Fail("Adiciona pelo menos um módulo.");
        if (planList.Count == 0)
            return ProductProfileResult.Fail("Adiciona pelo menos um plano.");

        await using GeneratorDbContext db = _dbContextFactory();

        bool exists;
        try
        {
            exists = await db.ProductProfiles.AnyAsync(p => p.ProductId == productId);
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return ProductProfileResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        if (exists)
            return ProductProfileResult.Fail("Já existe um produto com este identificador.");

        var profile = new ProductProfile
        {
            ProductId = productId,
            Name = name,
            AvailableFeatures = featureList,
            AvailablePlans = planList
        };

        db.ProductProfiles.Add(profile);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return ProductProfileResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        return ProductProfileResult.Ok(profile);
    }

    /// <summary>
    /// <c>ProductId</c> é deliberadamente imutável — é o valor gravado em
    /// qualquer licença já emitida com este perfil; mudá-lo depois quebraria
    /// a correspondência dessas licenças. Só <c>Name</c>, módulos e planos
    /// são editáveis.
    /// </summary>
    public async Task<ProductProfileResult> UpdateAsync(int id, string name, IEnumerable<string> features, IEnumerable<string> plans)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return ProductProfileResult.Fail("Indica o nome do produto.");

        List<string> featureList = features.Select(f => f.Trim()).Where(f => f.Length > 0).Distinct().ToList();
        List<string> planList = plans.Select(p => p.Trim()).Where(p => p.Length > 0).Distinct().ToList();

        if (featureList.Count == 0)
            return ProductProfileResult.Fail("Adiciona pelo menos um módulo.");
        if (planList.Count == 0)
            return ProductProfileResult.Fail("Adiciona pelo menos um plano.");

        await using GeneratorDbContext db = _dbContextFactory();

        ProductProfile? profile;
        try
        {
            profile = await db.ProductProfiles.FindAsync(id);
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return ProductProfileResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        if (profile is null)
            return ProductProfileResult.Fail("Perfil não encontrado.");

        profile.Name = name;
        profile.AvailableFeatures = featureList;
        profile.AvailablePlans = planList;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return ProductProfileResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        return ProductProfileResult.Ok(profile);
    }

    /// <summary>Nunca há delete físico — só marca <see cref="ProductProfile.IsActive"/> como falso. Licenças já emitidas com este perfil continuam válidas.</summary>
    public async Task<ProductProfileResult> DeactivateAsync(int id)
    {
        await using GeneratorDbContext db = _dbContextFactory();

        ProductProfile? profile;
        try
        {
            profile = await db.ProductProfiles.FindAsync(id);
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return ProductProfileResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        if (profile is null)
            return ProductProfileResult.Fail("Perfil não encontrado.");

        profile.IsActive = false;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return ProductProfileResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        return ProductProfileResult.Ok(profile);
    }

    public async Task<ProductProfileResult> ReactivateAsync(int id)
    {
        await using GeneratorDbContext db = _dbContextFactory();

        ProductProfile? profile;
        try
        {
            profile = await db.ProductProfiles.FindAsync(id);
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return ProductProfileResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        if (profile is null)
            return ProductProfileResult.Fail("Perfil não encontrado.");

        profile.IsActive = true;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return ProductProfileResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        return ProductProfileResult.Ok(profile);
    }

    /// <summary>
    /// Chamar uma vez no arranque da app (junto com <see cref="AuthService.EnsureDatabaseCreatedAsync"/>).
    /// Semeia os 3 produtos já descritos na Secção 9 do PDF original — idempotente:
    /// só semeia se a tabela estiver vazia, nunca sobrescreve edições manuais.
    /// </summary>
    public async Task EnsureSeedDataAsync()
    {
        await using GeneratorDbContext db = _dbContextFactory();
        if (await db.ProductProfiles.AnyAsync())
            return;

        string[] defaultPlans = ["Padrão", "Profissional", "Corporativo"];

        db.ProductProfiles.AddRange(
            new ProductProfile
            {
                ProductId = "schoolmanager.desktop_v01",
                Name = "School Manager",
                AvailableFeatures = ["Alunos", "Propinas", "Financeiro", "Relatorios"],
                AvailablePlans = defaultPlans.ToList()
            },
            new ProductProfile
            {
                ProductId = "smartgest.desktop_v01",
                Name = "SmartGest",
                AvailableFeatures = ["Contabilidade", "IVA/Impostos", "Inventario", "Relatorios"],
                AvailablePlans = defaultPlans.ToList()
            },
            new ProductProfile
            {
                ProductId = "kivenda.desktop_v03",
                Name = "KiVenda",
                AvailableFeatures = ["Caixa", "Estoque", "Compras", "Vendas"],
                AvailablePlans = defaultPlans.ToList()
            });

        await db.SaveChangesAsync();
    }
}
