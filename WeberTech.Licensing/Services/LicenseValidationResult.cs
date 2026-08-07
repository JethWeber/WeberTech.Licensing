using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Enums;

namespace WeberTech.Licensing.Services;

/// <summary>
/// Resultado de <see cref="LicenseValidator.Validate"/>: o
/// <see cref="LicenseStatus"/> calculado, mais a <see cref="License"/>
/// desserializada quando a assinatura verifica (mesmo que o status final
/// não seja <see cref="LicenseStatus.Valid"/> — ex.: útil para mostrar
/// "expirou em ..." numa licença com assinatura válida mas
/// <see cref="LicenseStatus.Expired"/>). É <c>null</c> quando o status é
/// <see cref="LicenseStatus.NotFound"/> ou <see cref="LicenseStatus.Invalid"/>.
/// </summary>
public sealed record LicenseValidationResult(LicenseStatus Status, License? License);
