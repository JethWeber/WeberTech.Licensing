using System.Security.Cryptography;
using WeberTech.Licensing.Entities;
using WeberTech.Licensing.Enums;

namespace WeberTech.Licensing.Tests.Fakes;

/// <summary>
/// Monta objetos <see cref="License"/> de exemplo e pares de chaves RSA
/// efémeros (gerados em memória, nunca gravados em disco) para os testes de
/// <c>LicenseIssuer</c> e <c>LicenseValidator</c>.
/// </summary>
internal static class LicenseTestData
{
    public static RSA CreateEphemeralKeyPair() => RSA.Create(2048); // menor que os 3072 de produção só para testes correrem mais rápido

    public static License CreateSample(
        string productId = "kivenda.desktop_v03",
        string machineId = "9F2C7A1E4B6D0083",
        DateTime? expiresAt = null) => new()
    {
        LicenseId = Guid.Parse("6a4f9e0a-1d2b-4a3f-9c7e-0a1b2c3d4e5f"),
        ProductId = productId,
        CustomerId = "00045",
        CustomerName = "Loja Central Lda",
        MachineId = machineId,
        Plan = "professional",
        Type = LicenseType.Subscription,
        Features = ["Caixa", "Estoque", "Compras", "Vendas"],
        IssuedAt = new DateTime(2026, 7, 30, 10, 0, 0, DateTimeKind.Utc),
        ExpiresAt = expiresAt ?? new DateTime(2027, 7, 30, 10, 0, 0, DateTimeKind.Utc)
    };
}
