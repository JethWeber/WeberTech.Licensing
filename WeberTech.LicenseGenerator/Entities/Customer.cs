namespace WeberTech.LicenseGenerator.Entities;

/// <summary>
/// Cliente para quem uma licença é emitida (M4, Fase 6) — não confundir
/// com <c>Entities.User</c> (staff da Weber Tech que usa a ferramenta).
/// Substitui o texto livre que <c>License.CustomerName</c>/<c>CustomerId</c>
/// (Core, Fase 5) recebiam antes: a partir do M6, o formulário de emissão
/// escolhe/cria um <see cref="Customer"/> daqui.
/// </summary>
public sealed class Customer
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>NIF ou equivalente — opcional (nem todo cliente tem à mão no momento da emissão).</summary>
    public string? TaxId { get; set; }

    /// <summary>Email/telefone de contacto — opcional.</summary>
    public string? Contact { get; set; }

    /// <summary>Permite "desativar" um cliente sem apagar licenças/histórico já ligados a ele — nunca há delete físico aqui.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
