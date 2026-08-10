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

## Estado atual: Fase 6 — M1 a M5 concluídos

**M1/M2/M3/M4:** ✅ (ver histórico)

**M5 — Cadastro de Produtos:** ✅ implementado, ⏳ aguardando validação.
- [x] **Decisão registada:** `ProductProfile` vive só no `LicenseGenerator`
      (não no Core) — o `ILicenseGate` do lado do produto-cliente só
      precisa de `HasFeature(string)` contra a licença já emitida; a lista
      de módulos *disponíveis* por produto só importa na hora de emitir.
- [x] `Entities/ProductProfile.cs` — `ProductId`, `Name`,
      `AvailableFeatures`/`AvailablePlans` (`List<string>`), `IsActive`
      (desativar, nunca apagar — licenças já emitidas com um perfil
      continuam válidas mesmo que ele saia de circulação).
- [x] `Persistence/GeneratorDbContext.cs` — `DbSet<ProductProfile>`, índice
      único em `ProductId`. `List<string>` gravado como texto delimitado
      por `|` via `ValueConverter` + `ValueComparer` (SQLite não tem tipo
      de coleção nativo — sem o `ValueComparer`, o EF não deteta mudanças
      *dentro* da lista corretamente).
- [x] `Services/ProductProfileService.cs` — `ListAsync`, `GetByProductIdAsync`,
      `CreateAsync`, e `EnsureSeedDataAsync()` (idempotente — só semeia se a
      tabela estiver vazia) com os **3 produtos da Secção 9 do PDF
      original**: School Manager (Alunos/Propinas/Financeiro/Relatorios),
      SmartGest (Contabilidade/IVA-Impostos/Inventario/Relatorios), KiVenda
      (Caixa/Estoque/Compras/Vendas), todos com planos Padrão/Profissional/
      Corporativo (só o mockup mostrava esses nomes de plano — o PDF nunca
      fixou um conjunto canónico, então esta é uma escolha, não um dado
      "oficial").
- [x] `ViewModels/ProductProfilePickerViewModel.cs` + `Views/ProductProfilePickerView.axaml`
      — busca local (poucos produtos, sem round-trip por letra) + "+ Novo
      produto" inline (identificador, nome, módulos e planos separados por
      vírgula). **Reutilizável de verdade**, como o `CustomerPickerView` —
      o M6 herda isto.
- [x] `IssueLicenseCheckpointViewModel`/`View` — o checkpoint do M4 evoluiu
      para mostrar os dois pickers lado a lado (cliente + produto), em vez
      de ser recriado do zero. Arquivos antigos `IssueLicenseM4Checkpoint*`
      removidos.
- [x] `App.axaml.cs` — `ProductProfileService.EnsureSeedDataAsync()` chamado
      no arranque, junto com `AuthService.EnsureDatabaseCreatedAsync()`.

**Adiantado antes do M9 (a pedido, entre M5 e M6) — `UpdateAsync`/`DeactivateAsync`
em Cliente e Produto:**
- [x] `Entities/Customer.cs` ganhou `IsActive` (não existia — só o
      `ProductProfile` tinha, e nem esse estava implementado de facto: o
      comentário prometia "desativar" mas não havia método nenhum).
- [x] `CustomerService`/`ProductProfileService` — `UpdateAsync`,
      `DeactivateAsync` (nunca delete físico — licenças/histórico já
      ligados continuam intactos), `ReactivateAsync`. `ListAsync` de
      ambos passa a filtrar só ativos por padrão (`activeOnly = true`).
      `ProductProfile.UpdateAsync` mantém `ProductId` imutável de
      propósito — é o valor gravado em licenças já emitidas.
- [x] `CustomerPickerViewModel`/`ProductProfilePickerViewModel` — botões
      "Editar"/"Desativar" ao lado de "+ Novo ...", habilitados só com um
      item selecionado. Painel de edição reaproveita os mesmos campos do
      painel de criação (evita duplicar estado). Nova propriedade
      computada `IsBrowsing` esconde lista/botões enquanto qualquer
      painel (criar ou editar) está aberto.
- ⚠️ **Isto NÃO é a tela de gestão do M9** — é só o suficiente pra não
  bloquear teste antes do M6. Sem confirmação modal ao desativar (clica e
  já desativa); isso fica pro M9, quando existir uma tela dedicada.

⚠️ **Atenção — o mesmo problema do `generator.db` desatualizado vai
acontecer de novo aqui**, porque `Customer` ganhou uma coluna nova
(`IsActive`). Se já tinhas corrido a app depois do M5, apaga o
`generator.db` local mais uma vez antes de testar isto (ver "Solução de
problemas conhecidos" abaixo).

## Solução de problemas conhecidos

**`SqliteException: no such table: X` (ou `no such column: X`) ao correr
`dotnet run`.** Causa: `EnsureCreated()` (usado no arranque, ver
`AuthService.EnsureDatabaseCreatedAsync`) só cria o esquema inteiro **na
primeira vez** que o ficheiro `.db` é gerado — não faz atualização
incremental, nem de tabelas novas nem de colunas novas em tabelas
existentes. Se já correste a app numa fase anterior e um marco novo
adicionou uma entidade (ou só um campo a uma entidade já existente — foi
o caso do `Customer.IsActive`, entre M5 e M6), o `generator.db` antigo
continua desatualizado.

**Solução:** apaga o ficheiro local e deixa recriar do zero (só tem dados
de teste, nunca dados de cliente real):
- Linux: `rm ~/.local/share/WeberTech/LicenseGenerator/generator.db`
- Windows: apaga `%LocalAppData%\WeberTech\LicenseGenerator\generator.db`

Isto vai continuar a acontecer a cada marco (ou ajuste dentro de um
marco) que mexer no `GeneratorDbContext`, enquanto não migrarmos para EF
Core Migrations formais (decisão consciente — ver nota no M2: esperar o
esquema estabilizar primeiro, para não gerar uma migration nova a cada
marco). O erro já vem com uma mensagem clara apontando exatamente este
ficheiro, em vez de uma stack trace crua do SQLite — mas só cobre "no
such table"; "no such column" (como neste caso) ainda aparece cru. Se
isto continuar a incomodar, é sinal de que vale a pena migrar para EF
Core Migrations de vez (o pacote `Microsoft.EntityFrameworkCore.Design`
já está referenciado no `.csproj`, preparado para isso).

## Estado atual: Fase 6 — M1 a M5 concluídos · M6 concluído

**M1–M5:** ✅ (ver histórico)

**M6 — Tela "Emitir Licença" (formulário completo):** ✅ implementado,
⏳ aguardando validação — **primeira emissão de `.wta` real através da UI**.

- [x] `Services/FileDialogService.cs` — diálogos nativos via
      `IStorageProvider` do Avalonia: `PickPrivateKeyFileAsync` (filtro
      `*.pem`) e `PickSaveWtaFileAsync` (filtro `*.wta`, nome sugerido a
      partir de cliente+produto). Recebe o `TopLevel` via delegate — não
      guarda referência direta a nenhuma janela.
- [x] `ViewModels/FeatureSelection.cs` — item de checkbox (nome do módulo
      + selecionado), populado a partir de `ProductProfile.AvailableFeatures`
      sempre que o produto selecionado muda.
- [x] `ViewModels/IssueLicenseViewModel.cs` — o formulário de verdade:
  - Embrulha `CustomerPickerViewModel` (M4) e `ProductProfilePickerViewModel`
    (M5) sem alterar nenhum dos dois — só escuta `ProductSelected` para
    reconstruir `AvailablePlans`/`Features` a partir do produto escolhido.
  - **Decisão consciente, diferente do mockup original:** a busca de
    produto vem pré-filtrada pelo `productId` decodificado do QR
    (`ProductPicker.SearchText = request.ProductId`), mas **não trava** a
    escolha — o mockup trata esse campo como imutável/auto-preenchido; aqui
    fica como sugestão forte, porque o picker é genérico e reutilizável
    (travar exigiria um "modo bloqueado" só para este caso).
  - Validação completa antes de emitir: cliente, produto, plano, data de
    validade (dispensada se `Perpetual`), caminho da chave privada, pelo
    menos um módulo marcado.
  - `IssueAsync` monta a `License` (Core, Fase 5) com os dados reais dos
    pickers + `Request.MachineId` (do QR, Fase 4), chama
    `LicenseIssuer.Issue(...)`, pede onde gravar via `FileDialogService`,
    grava com `LicenseStore.Save` (Core, Fase 5). `KeyLoadException` da
    chave privada vira `ErrorMessage`, não crash.
- [x] `Views/IssueLicenseView.axaml` — cliente+produto lado a lado (M4/M5
      embutidos), depois plano/tipo/datas/módulos, depois chave privada
      (caminho somente-leitura + "Procurar...", password mascarada), depois
      mensagem + botão final. `DatePicker.SelectedDate` para as datas;
      "Válido até" desabilitado quando `IsPerpetual`.
- [x] `NavigationShellViewModel` — construtor ganhou `Func<TopLevel?>`
      (vem do `App.axaml.cs`, que passa `() => mainWindow` — `Window`
      já É um `TopLevel`). "Emitir Licença" agora abre o formulário real
      em vez do checkpoint.
- [x] `IssueLicenseCheckpoint*` (M4+M5) removidos — totalmente substituídos.

⚠️ **Nada disto grava histórico ainda** — emitir gera o `.wta` de verdade
em disco, mas não fica registado em lado nenhum dentro da app (isso é
exatamente o M7). Se precisares de conferir uma licença emitida, por
agora só abrindo o ficheiro `.wta` gerado.

## Próximo passo: M7 — Histórico de Licenças

1. Entidade local (`EmissionHistoryEntry` ou similar) no `GeneratorDbContext`
   — data de emissão, cliente, produto/plano, Machine ID, expiração, status.
2. `IssueLicenseViewModel.IssueAsync` grava uma entrada aqui logo após
   `LicenseStore.Save` ter sucesso.
3. Tabela com filtros por produto/status/data (mockup: `Histórico de
   Licenças`), substitui o `PlaceholderView` que "Histórico" mostra hoje.

Ver plano completo (M0–M11) na conversa.
