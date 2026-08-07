namespace WeberTech.Licensing.Enums;

/// <summary>
/// Resultado da validação de um ficheiro .wta (ver <see cref="Services.LicenseValidator"/>,
/// Secção 8.4 do roteiro).
/// </summary>
public enum LicenseStatus
{
    /// <summary>Nenhum ficheiro .wta encontrado no caminho esperado.</summary>
    NotFound,

    /// <summary>Assinatura não verifica com a chave pública embutida — payload adulterado, corrompido, ou assinado com outra chave.</summary>
    Invalid,

    /// <summary>Assinatura válida, mas a licença foi emitida para outro produto.</summary>
    ProductMismatch,

    /// <summary>Assinatura válida, mas a licença foi emitida para outra máquina.</summary>
    MachineMismatch,

    /// <summary>Assinatura e correspondência de produto/máquina corretas, mas a data de validade já passou.</summary>
    Expired,

    /// <summary>Tudo confere — licença ativa e utilizável.</summary>
    Valid
}
