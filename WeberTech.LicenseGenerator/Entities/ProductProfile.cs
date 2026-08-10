namespace WeberTech.LicenseGenerator.Entities;

/// <summary>
/// Perfil de produto — módulos e planos disponíveis para emissão de
/// licença (M5, Fase 6). Vive só no <c>LicenseGenerator</c>, por decisão
/// explícita: o que importa do lado do produto-cliente (<c>ILicenseGate</c>,
/// Core) é só <c>HasFeature(string)</c> contra a licença já emitida — a
/// lista de módulos *disponíveis* por produto só interessa na hora de
/// emitir, então não precisa viajar até ao Core nem até aos clientes.
/// </summary>
public sealed class ProductProfile
{
    public int Id { get; set; }

    /// <summary>Ex.: "kivenda.desktop_v03" — mesmo valor que vai em <c>License.ProductId</c> (Core, Fase 5).</summary>
    public required string ProductId { get; set; }

    public required string Name { get; set; }

    public List<string> AvailableFeatures { get; set; } = new();

    public List<string> AvailablePlans { get; set; } = new();

    /// <summary>Permite "desativar" um perfil sem apagar licenças já emitidas com ele — nunca há delete físico aqui.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
