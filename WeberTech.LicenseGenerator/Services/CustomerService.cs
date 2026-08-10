using Microsoft.EntityFrameworkCore;
using WeberTech.LicenseGenerator.Entities;
using WeberTech.LicenseGenerator.Persistence;

namespace WeberTech.LicenseGenerator.Services;

/// <summary>Resultado de uma operação de criação de cliente — nunca lança por validação, só devolve <see cref="Success"/> = false.</summary>
public sealed record CustomerResult(bool Success, Customer? Customer, string? ErrorMessage)
{
    public static CustomerResult Ok(Customer customer) => new(true, customer, null);
    public static CustomerResult Fail(string errorMessage) => new(false, null, errorMessage);
}

/// <summary>
/// CRUD mínimo de clientes (M4, Fase 6) — listar/pesquisar e criar. Editar/
/// remover ficam para quando surgir necessidade real (não faz parte do
/// critério de "pronto" deste marco).
/// </summary>
public sealed class CustomerService
{
    private readonly Func<GeneratorDbContext> _dbContextFactory;

    public CustomerService() : this(() => new GeneratorDbContext()) { }

    public CustomerService(Func<GeneratorDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    /// <summary>
    /// Lista clientes, opcionalmente filtrados por nome (contém, case-insensitive
    /// via <c>EF.Functions.Like</c>). Limitado a 50 resultados — suficiente
    /// para um picker de busca-enquanto-digita, sem paginação por agora.
    /// </summary>
    public async Task<IReadOnlyList<Customer>> ListAsync(string? searchTerm = null, bool activeOnly = true)
    {
        await using GeneratorDbContext db = _dbContextFactory();
        IQueryable<Customer> query = db.Customers.OrderBy(c => c.Name);

        if (activeOnly)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim();
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{term}%"));
        }

        return await query.Take(50).ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        await using GeneratorDbContext db = _dbContextFactory();
        return await db.Customers.FindAsync(id);
    }

    public async Task<CustomerResult> CreateAsync(string name, string? taxId = null, string? contact = null)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length < 2)
            return CustomerResult.Fail("O nome do cliente precisa de pelo menos 2 caracteres.");

        await using GeneratorDbContext db = _dbContextFactory();

        var customer = new Customer
        {
            Name = name,
            TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim(),
            Contact = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim()
        };

        db.Customers.Add(customer);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return CustomerResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        return CustomerResult.Ok(customer);
    }

    public async Task<CustomerResult> UpdateAsync(int id, string name, string? taxId = null, string? contact = null)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length < 2)
            return CustomerResult.Fail("O nome do cliente precisa de pelo menos 2 caracteres.");

        await using GeneratorDbContext db = _dbContextFactory();

        Customer? customer;
        try
        {
            customer = await db.Customers.FindAsync(id);
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return CustomerResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        if (customer is null)
            return CustomerResult.Fail("Cliente não encontrado.");

        customer.Name = name;
        customer.TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim();
        customer.Contact = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim();

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return CustomerResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        return CustomerResult.Ok(customer);
    }

    /// <summary>Nunca há delete físico — só marca <see cref="Customer.IsActive"/> como falso. Licenças/histórico já ligados a este cliente continuam intactos.</summary>
    public async Task<CustomerResult> DeactivateAsync(int id)
    {
        await using GeneratorDbContext db = _dbContextFactory();

        Customer? customer;
        try
        {
            customer = await db.Customers.FindAsync(id);
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return CustomerResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        if (customer is null)
            return CustomerResult.Fail("Cliente não encontrado.");

        customer.IsActive = false;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return CustomerResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        return CustomerResult.Ok(customer);
    }

    public async Task<CustomerResult> ReactivateAsync(int id)
    {
        await using GeneratorDbContext db = _dbContextFactory();

        Customer? customer;
        try
        {
            customer = await db.Customers.FindAsync(id);
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return CustomerResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        if (customer is null)
            return CustomerResult.Fail("Cliente não encontrado.");

        customer.IsActive = true;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (DatabaseErrorHelper.IsDatabaseError(ex))
        {
            return CustomerResult.Fail(DatabaseErrorHelper.DescribeError(ex));
        }

        return CustomerResult.Ok(customer);
    }
}
