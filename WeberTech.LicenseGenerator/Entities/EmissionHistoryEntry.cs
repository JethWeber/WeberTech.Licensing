namespace WeberTech.LicenseGenerator.Entities;

/// <summary>
/// Registo local de uma licença emitida (M7, Fase 6) — histórico
/// operacional da Weber Tech, não a licença em si (essa é o `.wta`
/// gravado no disco do cliente, ver <see cref="WeberTech.Licensing.Storage.LicenseStore"/>).
///
/// Campos denormalizados de propósito: uma entrada de histórico é uma
/// fotografia do momento da emissão — não muda se o cliente ou o produto
/// forem editados depois (mesmo princípio do <c>ProductId</c> imutável em
/// <c>ProductProfileService.UpdateAsync</c>, M5).
/// </summary>
public sealed class EmissionHistoryEntry
{
    public int Id { get; set; }

    /// <summary>Mesmo valor de <c>License.LicenseId</c> (Core) — permite cruzar com o `.wta` se necessário.</summary>
    public required Guid LicenseId { get; set; }

    public required string ProductId { get; set; }
    public required string ProductName { get; set; }
    public required string CustomerName { get; set; }
    public required string Plan { get; set; }

    /// <summary>Guardado como string (ToString() do enum Core) — evita acoplar o esquema local ao enum do Core.</summary>
    public required string LicenseType { get; set; }

    public required string MachineId { get; set; }
    public required DateTime IssuedAt { get; set; }

    /// <summary><c>null</c> = licença perpétua.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Caminho onde o `.wta` foi gravado — útil para reencontrar o ficheiro em suporte.</summary>
    public required string FilePath { get; set; }
}
