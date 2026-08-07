using WeberTech.Licensing.Enums;

namespace WeberTech.Licensing.Entities;

/// <summary>
/// Payload de dados da licença (ver Secção 8.1 do roteiro). É este objeto
/// que vai, serializado e em Base64, dentro do envelope <see cref="LicenseFile"/>
/// gravado como <c>.wta</c>. Nunca é gravado nem transmitido em texto simples
/// fora do envelope — só a assinatura garante a integridade, o Base64 aqui é
/// só para não convidar a edição manual num editor de texto comum.
/// </summary>
public sealed class License
{
    public required Guid LicenseId { get; set; }

    /// <summary>Ex.: "kivenda.desktop_v03", "schoolmanager.desktop_v01".</summary>
    public required string ProductId { get; set; }

    public required string CustomerId { get; set; }
    public required string CustomerName { get; set; }

    /// <summary>Machine ID calculado por <see cref="Services.MachineIdService"/> no PC do cliente.</summary>
    public required string MachineId { get; set; }

    /// <summary>Ex.: "professional" — validado contra <c>ProductProfile.AvailablePlans</c> na emissão (Fase 6).</summary>
    public required string Plan { get; set; }

    public required LicenseType Type { get; set; }

    /// <summary>Módulos contratados — validados contra <c>ProductProfile.AvailableFeatures</c> na emissão (Fase 6).</summary>
    public required string[] Features { get; set; }

    public required DateTime IssuedAt { get; set; }

    /// <summary><c>null</c> = licença perpétua (<see cref="LicenseType.Perpetual"/>).</summary>
    public DateTime? ExpiresAt { get; set; }
}
