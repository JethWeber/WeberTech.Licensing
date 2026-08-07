using WeberTech.Licensing.Services;

namespace WeberTech.Licensing.Entities;

/// <summary>
/// Payload do pedido de ativação, transmitido via QR Code do produto-cliente
/// para a Weber Tech (ver Secção 7 do roteiro). Não contém a licença — só
/// informação suficiente para o <c>WeberTech.LicenseGenerator</c> saber
/// para que produto, versão e máquina deve emitir o ficheiro <c>.wta</c>.
/// </summary>
public sealed class ActivationRequest
{
    /// <summary>Marca de formato fixa, usada para validar o QR antes de tentar interpretá-lo.</summary>
    public string Fmt { get; set; } = ActivationRequestService.FormatTag;

    /// <summary>Ex.: "kivenda.desktop_v03", "schoolmanager.desktop_v01".</summary>
    public required string ProductId { get; set; }

    /// <summary>Machine ID calculado localmente por <see cref="Services.MachineIdService"/>.</summary>
    public required string MachineId { get; set; }

    /// <summary>Identificador único deste pedido específico (permite ao Generator detetar reutilização — Secção 13 do roteiro).</summary>
    public Guid RequestId { get; set; } = Guid.NewGuid();

    /// <summary>Instante (UTC) em que o pedido foi gerado no PC do cliente.</summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
