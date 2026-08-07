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
> para confirmar que tudo restaura e compila antes de avançar de fase.

### VS Code

O repositório já vem com `.vscode/` configurado — abre a **pasta raiz**
(`WeberTech.Licensing/`, a que contém o `.sln`) no VS Code, não uma subpasta.

1. **Extensões** — ao abrir, o VS Code vai sugerir instalar as recomendadas
   (`.vscode/extensions.json`): **C# Dev Kit** (IntelliSense, debugger, Test
   Explorer) e **Avalonia for VS Code** (preview dos `.axaml` da Fase 6).
   Aceita a sugestão, ou instala manualmente pelo separador de Extensões.
2. **Build** — `Ctrl+Shift+B` (task padrão) ou `Ctrl+Shift+P` → *Run Task* →
   `build`. As tasks `restore`, `test`, `watch-tests` e
   `publish LicenseGenerator (win-x64)` também estão em `.vscode/tasks.json`.
3. **Testes** — abre o separador **Testing** (ícone de frasco na barra
   lateral); o C# Dev Kit deteta os testes xUnit automaticamente e permite
   correr/depurar cada um individualmente, sem precisar de configuração.
4. **Debug do LicenseGenerator** — `F5` usa a configuração
   `Debug LicenseGenerator` (`.vscode/launch.json`). Corre normalmente no
   Mint (a UI Avalonia é multiplataforma, renderiza via Skia); só o caminho
   que chama `WmiQueryService` (Machine ID real) lança
   `PlatformNotSupportedException` aqui — ver secção abaixo, é esperado.

## Desenvolvimento em Linux (Mint), destino de execução em Windows

- **Compila e testa 100% no Mint.** Todos os testes usam fakes
  (`FakeWmiQueryService`, chaves RSA efémeras em memória) — nunca dependem
  de WMI real nem do Windows.
- **`WmiQueryService` só funciona em runtime no Windows.** É esperado — é o
  SO de todos os produtos-cliente. No Mint, compila normalmente; se
  chamares `QueryFirst` de verdade (fora dos testes), lança
  `PlatformNotSupportedException`.
- **Cross-publish para Windows a partir do Mint funciona sem precisar de
  uma máquina Windows para compilar:**
  ```bash
  dotnet publish WeberTech.LicenseGenerator -c Release -r win-x64 --self-contained true
  ```
  O `.csproj` já tem `<RuntimeIdentifiers>win-x64;win-x86</RuntimeIdentifiers>`
  declarado, e o SDK descarrega os runtime packs automaticamente no restore.
- **O que só dá para validar num Windows real:** o `MachineIdService` de
  verdade (WMI), e o fluxo completo do `LicenseGenerator` a ler QR por
  webcam (ZXing). Antes de qualquer emissão real de licença, corre pelo
  menos uma vez numa VM ou máquina Windows física.

## Estado atual: Fase 2 concluída (Criptografia)

- [x] **Padding RSA decidido e fixado: PSS** (`RSASignaturePadding.Pss`,
      `SHA256`). Ver `SignatureService.AlgorithmIdentifier` = `"RSA-SHA256-PSS"`,
      valor que será gravado no campo `algorithm` do envelope `.wta` na Fase 5.
      Esta decisão está fechada — não usar PKCS#1 v1.5 em nenhum código novo.
- [x] `Crypto/SignatureService.cs` — `Sign(payloadJson, privateKey)` /
      `Verify(payloadJson, signature, publicKey)`.
- [x] `Crypto/KeyProvider.cs` — `LoadPublicKey()` (lê o recurso embutido
      `wt_public.pem`, uso de qualquer produto-cliente) e
      `LoadPrivateKey(path, password?)` (lê PEM em disco, opcionalmente
      cifrado; uso exclusivo do `WeberTech.LicenseGenerator`).
- [x] `Exceptions/WeberTechLicensingException.cs` (base) e
      `Exceptions/KeyLoadException.cs`.
- [x] **Chave pública de DESENVOLVIMENTO** gerada localmente (3072 bits) e
      embutida em `WeberTech.Licensing/Properties/wt_public.pem` — **não é**
      a chave real da Weber Tech. A respetiva privada foi gerada apenas para
      derivar esta pública e foi destruída no mesmo passo; não existe em
      lado nenhum deste repositório ou dos ficheiros entregues.
      ⚠️ **Antes de emitir qualquer licença real**, gerar o par de chaves
      definitivo (Secção 4.1 do roteiro) e substituir este ficheiro.
- [x] Testes: `SignatureServiceTests` (round-trip, payload adulterado, chave
      errada, assinatura truncada, e confirmação de que uma assinatura PKCS#1
      não verifica através do serviço) e `KeyProviderTests` (leitura da
      pública embutida, privada não cifrada, privada cifrada com password
      certa/errada, caminho inexistente).

## Estado atual: Fases 1–4 concluídas (Setup, Criptografia, Machine ID, Ativação/QR)

**Fase 3 — Machine ID**
- [x] `Services/IWmiQueryService.cs` — abstração sobre WMI, existe só para tornar
      `MachineIdService` testável sem depender de hardware real nem do Windows.
- [x] `Services/WmiQueryService.cs` — implementação real (`System.Management`,
      `[SupportedOSPlatform("windows")]`); consulta `Win32_Processor`,
      `Win32_BaseBoard`, `Win32_DiskDrive`.
- [x] `Services/MachineIdService.cs` — `GetMachineId()` combina os três
      valores + `SHA256`, truncado a 32 caracteres hex.
- [x] `Exceptions/MachineIdentificationException.cs`.
- [x] Testes (`MachineIdServiceTests`, com `Fakes/FakeWmiQueryService`):
      determinismo, comprimento/formato do ID, sensibilidade a mudança de
      CPU/motherboard/disco, insensibilidade a periféricos externos.

**Fase 4 — Pedido de Ativação e QR Code**
- [x] `Entities/ActivationRequest.cs` — `Fmt`, `ProductId`, `MachineId`,
      `RequestId`, `RequestedAt`.
- [x] `Services/ActivationRequestService.cs` — `BuildQrText(...)` /
      `ParseQrText(...)`, formato `WTAREQ1:<base64(json)>`.
- [x] `Services/QrCodeService.cs` — `GeneratePng(qrText, pixelsPerModule)`
      via `QRCoder`, nível de correção de erro **Q** (~25%, margem para
      fotografia por telemóvel). Devolve `byte[]` PNG, não um tipo de UI —
      mantém o princípio "zero dependência de UI" do Core.
- [x] `Exceptions/ActivationRequestFormatException.cs`.
- [x] Testes: `ActivationRequestServiceTests` (round-trip, prefixo ausente,
      Base64 inválido, JSON incompleto) e `QrCodeServiceTests` (assinatura
      PNG válida, tamanho cresce com `pixelsPerModule`, argumentos inválidos).

⚠️ **Nota:** `WmiQueryService` só é funcional em Windows — é o SO de todos os
produtos-cliente desta arquitetura, então isso é esperado, não um bug. Os
testes usam sempre `FakeWmiQueryService`, por isso correm em qualquer SO.

## Estado atual: Fases 1–5 concluídas (Setup, Criptografia, Machine ID, Ativação/QR, Emissão/Validação do `.wta`)

**Fase 5 — Emissão e Validação do `.wta`**
- [x] `Entities/License.cs` — payload de dados da licença (`LicenseId`,
      `ProductId`, `CustomerId`, `CustomerName`, `MachineId`, `Plan`, `Type`,
      `Features`, `IssuedAt`, `ExpiresAt` nulo = perpétua).
- [x] `Entities/LicenseFile.cs` — envelope gravado como `.wta` (`Payload`,
      `Signature`, `Algorithm`, `KeyVersion`).
- [x] `Services/LicenseJsonOptions.cs` (interno) — opções de serialização
      partilhadas por Issuer/Validator/Store, para emissor e validador nunca
      divergirem na leitura do mesmo JSON (isso invalidaria assinaturas em
      silêncio). Enums serializados pelo nome (`"Subscription"`, não `1`).
- [x] `Services/LicenseIssuer.cs` — `Issue(license, privateKey)`. Uso
      exclusivo do `WeberTech.LicenseGenerator` (Fase 6).
- [x] `Services/LicenseValidator.cs` — `Validate(envelope, productId,
      machineId, publicKey)`, devolve `LicenseValidationResult(Status,
      License?)`. Ordem de verificação: assinatura → produto → máquina →
      expiração, conforme Secção 8.4 do roteiro. Nunca lança exceção por
      licença inválida — só por argumentos malformados.
- [x] `Storage/LicenseStore.cs` — `Save`/`Load` do `.wta` em
      `%ProgramData%\WeberTech\{productId}\license.wta`, mais
      `RecordSuccessfulVerification`/`GetLastSuccessfulVerification` (carimbo
      ao lado do ficheiro — mitigação de "recuar o relógio", Secção 9).
- [x] `Exceptions/LicenseFileCorruptedException.cs`.
- [x] Testes: `LicenseIssuerTests` (round-trip completo de todos os campos,
      incluindo licença perpétua com `ExpiresAt` nulo), `LicenseValidatorTests`
      (os 6 valores de `LicenseStatus`, incluindo assinatura adulterada,
      payload adulterado, chave errada, e a ordem produto-antes-de-máquina),
      `LicenseStoreTests` (save/load, criação de subpastas, ficheiro
      corrompido, carimbo de verificação, caminho padrão).

## Estado atual: Fase 6 redesenhada (mockups do utilizador) — M1 concluído, aguardando validação visual

A Fase 6 foi reformulada a partir de mockups fornecidos (design system
**Cyber-Shield Enterprise**: navy escuro + azul elétrico, Inter + JetBrains
Mono). Plano completo em 12 marcos (M0–M11) combinado na conversa — decisão
de plataforma (M0): **desktop Avalonia**, não web. O trabalho das Fases 1–5
(Core: Crypto, Machine ID, `.wta`) não muda em nada — só a camada de UI do
`LicenseGenerator` está a ser refeita.

**M0 — Decisão de plataforma:** ✅ Desktop Avalonia (confirmado).

**M1 — Fundação visual (design tokens):** ✅ implementado, ⏳ aguardando
confirmação visual antes de M2.
- [x] Fonte **JetBrains Mono** (Regular/Medium/SemiBold/Bold, licença OFL)
      baixada do repositório oficial e embutida em
      `Assets/Fonts/JetBrainsMono/*.ttf` — não depende de estar instalada
      no PC do cliente. Inter já vinha via `Avalonia.Fonts.Inter`
      (`WithInterFont()`, Program.cs, desde a Fase 1).
- [x] `Styles/DesignTokens.axaml` — todas as cores do `DESIGN.md` como
      `Color`/`SolidColorBrush`, tamanhos de tipografia, radius (2/4/6/8px),
      spacing (unidade base 4px).
- [x] `Styles/Styles.axaml` — classes reutilizáveis: `TextBlock` (`.display-lg`,
      `.headline-lg/md`, `.body-lg/sm`, `.label-md`, `.mono-md`), `Border`
      (`.card`, `.mono-well` para chaves/IDs, `.badge`/`.badge-success`/`.badge-error`),
      `Button` (`.accent`/`.secondary`/`.tertiary`, com glow simples no hover
      do `.accent` — `BoxShadow` azul a 30%, ver Secção "Elevação" do
      `DESIGN.md`), `TextBox` com borda azul no foco.
- [x] `App.axaml` — `RequestedThemeVariant="Dark"`, tokens mergeados em
      `Application.Resources`, `Styles.axaml` incluído depois do
      `FluentTheme` (pra conseguir sobrepor os defaults).
- [x] `Views/DesignSystemPreview.axaml` — tela de verificação temporária
      (card + badges + 3 variantes de botão + texto em `mono-md`),
      atualmente é o que a `MainWindow` mostra. **Não é uma tela final** —
      só existe para confirmar visualmente antes do M2.
- [ ] **Ainda por confirmar:** abrir no VS Code (Avalonia Previewer) ou
      rodar (`F5`) e conferir se cores/fontes batem com os mockups.

⚠️ **Nota de segurança à frente (M9):** o mockup de Configurações mostra uma
"Chave Mestra" em texto copiável. Isso conflita com a arquitetura RSA
assimétrica já construída (Secção 4 do roteiro) — a chave privada nunca
pode aparecer em texto nem ser copiável. Vamos adaptar essa tela para
mostrar metadados da chave (fingerprint/versão/data), não o valor — já
sinalizado no plano, ainda não é bloqueante agora.

## Próximo passo: M2 — Autenticação (Login/Register)

1. Entidade `User` (nome, username/email, password hash) — provavelmente em
   `WeberTech.LicenseGenerator/Persistence/`, junto com o histórico de
   emissões (SQLite local).
2. Hashing de password (ex.: `BCrypt.Net` ou `Rfc2898DeriveBytes`/PBKDF2 do
   BCL, evitar dependência extra se der).
3. `Views/LoginView.axaml` + `Views/RegisterView.axaml` seguindo o visual
   já estabelecido no M1 (`.card`, `.accent`, `TextBox` estilizado).
4. Sessão simples em memória — nenhuma tela pós-login acessível sem
   autenticação válida.

Ver plano completo (M0–M11) na conversa — ainda não copiado para o roteiro
`.md` principal; farei isso quando a Fase 6 estiver mais estável, para não
reescrever o documento a cada marco.
