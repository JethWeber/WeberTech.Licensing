# WeberTech.Licensing

Sistema de licenciamento offline com chaves assimétricas, ativação por QR Code
e ficheiros de licença `.wta`. Ver `WeberTech_Licensing_Roteiro_Implementacao.md`
para o roteiro técnico completo (todas as fases).

## Estado atual: Fase 1 concluída (Setup)

- [x] `WeberTech.Licensing.sln` com os três projetos ligados.
- [x] `WeberTech.Licensing` (Core) — csproj configurado (`net10.0`, `Nullable`,
      `ImplicitUsings`), `GlobalUsings.cs`, estrutura de pastas
      (`Entities/`, `Enums/`, `Models/`, `Crypto/`, `Services/`, `Storage/`,
      `Exceptions/`), pacotes `QRCoder` e `System.Management` referenciados.
- [x] `WeberTech.LicenseGenerator` (Avalonia) — shell mínimo a compilar
      (`Program.cs`, `App.axaml`, `App.axaml.cs`, `app.manifest`), referência
      de projeto ao Core, pacotes `ZXing.Net`, `ZXing.Net.Bindings.SkiaSharp`
      e `Microsoft.EntityFrameworkCore.Sqlite` referenciados, estrutura de
      pastas (`Views/`, `ViewModels/`, `Services/`, `Persistence/`, `Assets/`).
- [x] `WeberTech.Licensing.Tests` (xUnit) — csproj configurado, referência de
      projeto ao Core, estrutura de pastas (`Crypto/`, `Services/`, `Fakes/`).

## Como abrir

```bash
dotnet restore WeberTech.Licensing.sln
dotnet build WeberTech.Licensing.sln
```

> Este ambiente de geração não tem o SDK do .NET instalado, por isso os
> comandos acima não foram executados aqui — corre-os no teu ambiente local
> para confirmar que tudo restaura e compila antes de avançar para a Fase 2.

## Próximo passo: Fase 2 — Criptografia (RSA)

1. Gerar o par de chaves definitivo (3072 bits), offline, fora do repositório.
2. Guardar `wt_private.pem` protegida (nunca commitada — já coberta pelo
   `.gitignore`).
3. Colocar `wt_public.pem` em `WeberTech.Licensing/Properties/` (o `.csproj`
   já está preparado para embuti-la automaticamente se o ficheiro existir).
4. Implementar `Crypto/KeyProvider.cs` e `Crypto/SignatureService.cs`.

Ver Secção 4 do roteiro para o código de referência e a decisão pendente
sobre o padding RSA (PKCS#1 v1.5 vs PSS) antes de avançar.
