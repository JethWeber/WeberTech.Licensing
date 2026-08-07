using System.Security.Cryptography;

namespace WeberTech.LicenseGenerator.Services;

/// <summary>
/// Hashing de password via PBKDF2-HMACSHA256, usando só a biblioteca
/// padrão do .NET (sem BCrypt.Net ou similar — evita dependência extra
/// para algo que o BCL já resolve bem).
///
/// Formato de saída auto-contido: <c>{iterações}.{saltBase64}.{hashBase64}</c>
/// — o número de iterações fica gravado ao lado de cada hash, para poder
/// ser aumentado no futuro (hardware mais rápido) sem invalidar contas
/// já existentes.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;

    /// <summary>210.000 iterações — recomendação OWASP (2023+) para PBKDF2-HMAC-SHA256.</summary>
    private const int Iterations = 210_000;

    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password não pode ser vazia.", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySizeBytes);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    /// <summary>
    /// Nunca lança por hash malformado — devolve <c>false</c>, tratado como
    /// credenciais inválidas pelo chamador (mesmo princípio de "mensagem
    /// genérica" usado no <c>AuthService</c>).
    /// </summary>
    public static bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        string[] parts = hash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations))
            return false;

        byte[] salt, expectedKey;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedKey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);

        // Comparação em tempo constante — evita vazar informação por timing attack.
        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
