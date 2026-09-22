namespace WeberTech.Licensing.Models;

public sealed record LicenseInfo(
    Guid LicenseId,
    string ProductId,
    string CustomerId,
    string CustomerName,
    string MachineId,
    string Plan,
    DateTime IssuedAt,
    DateTime? ExpiresAt);
