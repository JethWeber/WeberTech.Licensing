# WeberTech.Licensing — Roteiro de Implementação (Versão Final Consolidada)

> Base: `WeberTech_Licensing_Documentacao_V01.pdf` (versão 1.0, Julho de 2026).
> Este ficheiro é a única fonte de verdade para a implementação do sistema de licenciamento, a partir de agora.
> Modelo: sistema de duas partes — **Emissor** (`WeberTech.LicenseGenerator`, fica na Weber Tech, possui a chave privada) e **Validador embutido** (`WeberTech.Licensing`, vai dentro de cada produto, possui a chave pública) — comunicando através de um ficheiro `.wta` assinado digitalmente, cujo pedido inicial viaja por QR Code.

---

## 0. Princípios Arquiteturais

1. **Licenciamento é um produto interno, não um módulo.** `WeberTech.Licensing` não pertence a nenhum produto (School Manager, KiVenda, SmartGest); é referenciado por todos eles como uma DLL externa.
2. **Separação estrita Core / UI / Ferramenta do emissor.** `WeberTech.Licensing` é uma Class Library pura (sem Avalonia). `WeberTech.LicenseGenerator` é a única aplicação que possui a chave privada — nunca é distribuída ao cliente.
3. **A chave privada nunca sai da Weber Tech.** É gerada uma única vez, offline, e vive protegida (PEM cifrado ou DPAPI) apenas no ambiente do `LicenseGenerator`. Cada produto embute apenas a chave **pública**.
4. **100% offline em produção.** Nenhuma chamada de rede é necessária para ativar, validar ou operar uma licença. QR Code + ficheiro `.wta` são os únicos "canais" de transporte, e ambos podem circular por WhatsApp/email/pen sem depender de internet no PC do cliente.
5. **Um perfil por produto (`ProductProfile`).** Módulos e planos disponíveis são definidos centralmente, tornando impossível ao operador emitir uma combinação inválida (ex.: módulo "Alunos" numa licença de KiVenda).
6. **API pública minimalista.** A superfície exposta a quem integra (interno ou terceiro) resume-se a `Licensing.Initialize`, `CurrentStatus`, `GenerateActivationQrCode`, `ImportLicenseFile`, `HasFeature`, `DaysUntilExpiration`, `GetLicenseInfo`.
7. **Preparado para distribuição como SDK.** Desde a Fase 1, a estrutura de pastas e os nomes de projeto já assumem que, no futuro, `WeberTech.Licensing` será empacotado como pacote NuGet privado para terceiros — sem precisar de reestruturação.
8. **Extensibilidade sem quebra de contrato.** `WeberTech.Licensing.Api`, `.CLI` e `.SDK` são extensões futuras que reaproveitam o mesmo formato `.wta`; o modo híbrido (validação online opcional com fallback offline) não pode alterar esse formato.

---

## 1. Estrutura de Pastas

```
WeberTech.Licensing.sln
│
├── WeberTech.Licensing/                        (Core — Class Library, sem UI)
│   ├── WeberTech.Licensing.csproj
│   ├── GlobalUsings.cs
│   │
│   ├── Entities/
│   │   ├── License.cs                          (modelo do payload da licença)
│   │   └── ActivationRequest.cs                (modelo do pedido de ativação / QR)
│   │
│   ├── Enums/
│   │   ├── ProductType.cs                      (SchoolManager, SmartGest, KiVenda)
│   │   ├── LicenseType.cs                      (Trial, Perpetual, Subscription)
│   │   └── LicenseStatus.cs                    (Valid, Expired, MachineMismatch, ProductMismatch, Invalid, NotFound)
│   │
│   ├── Models/
│   │   └── ProductProfile.cs                   (módulos/planos disponíveis por produto)
│   │
│   ├── Crypto/
│   │   ├── SignatureService.cs                 (assinar/verificar RSA-SHA256)
│   │   └── KeyProvider.cs                      (carrega chave pública embutida / privada protegida)
│   │
│   ├── Services/
│   │   ├── LicenseValidator.cs                 (valida assinatura + regras)
│   │   ├── LicenseIssuer.cs                    (usado só pelo Generator; cria e assina .wta)
│   │   ├── MachineIdService.cs                 (impressão digital do hardware)
│   │   ├── ActivationRequestService.cs         (cria/lê o payload do QR)
│   │   └── QrCodeService.cs                    (gera a imagem do QR — QRCoder)
│   │
│   ├── Storage/
│   │   └── LicenseStore.cs                     (lê/grava o .wta no disco local do cliente)
│   │
│   ├── Exceptions/
│   │   ├── WeberTechLicensingException.cs      (base)
│   │   ├── LicenseFileNotFoundException.cs
│   │   ├── InvalidSignatureException.cs
│   │   └── UnknownProductProfileException.cs
│   │
│   └── Licensing.cs                            (fachada estática — API pública, Secção 10)
│
├── WeberTech.LicenseGenerator/                 (Avalonia — app do emissor, possui a chave privada)
│   ├── Views/
│   │   ├── ActivationView.axaml                (Passo 1 — ler o pedido / QR)
│   │   ├── IssueLicenseView.axaml               (Passo 2/3 — confirmar dados + emitir)
│   │   └── HistoryView.axaml                    (histórico de licenças emitidas)
│   ├── ViewModels/
│   │   ├── ActivationViewModel.cs
│   │   ├── IssueLicenseViewModel.cs
│   │   └── HistoryViewModel.cs
│   ├── Services/
│   │   ├── QrReaderService.cs                  (ZXing.Net — webcam ou imagem)
│   │   └── FileDialogService.cs
│   ├── Persistence/
│   │   └── EmissionHistoryDbContext.cs          (SQLite local — histórico de licenças emitidas)
│   └── Assets/
│
└── WeberTech.Licensing.Tests/                  (xUnit)
    ├── Crypto/SignatureServiceTests.cs
    ├── Services/MachineIdServiceTests.cs
    ├── Services/ActivationRequestServiceTests.cs
    ├── Services/LicenseIssuerTests.cs
    ├── Services/LicenseValidatorTests.cs
    └── Fakes/{FakeKeyProvider, FakeClock}.cs
```

Consumo por parte de cada produto (fora desta solução, mas relevante para a integração):

```
SchoolManager  ──┬── SchoolManager.Core
                 └── WeberTech.Licensing            (via WeberTechLicenseGate : ILicenseGate)

SmartGest      ──┬── SmartGest.Core
                 └── WeberTech.Licensing

KiVenda        ──┬── KiVenda.Core
                 └── WeberTech.Licensing
```

---

## 2. Mapa de Componentes → Responsabilidades

| Componente                     | Vive em                     | Responsabilidade                                                                                     |
| ------------------------------- | ---------------------------- | ------------------------------------------------------------------------------------------------------ |
| `License`                       | `WeberTech.Licensing`        | Payload de dados da licença (`licenseId`, `productId`, `customerId`, `machineId`, `plan`, `features`, `issuedAt`, `expiresAt`). |
| `ActivationRequest`             | `WeberTech.Licensing`        | Payload do pedido de ativação (`fmt`, `productId`, `machineId`, `requestId`, `requestedAt`).           |
| `LicenseFile` (envelope `.wta`) | `WeberTech.Licensing`        | `payload` (Base64), `signature` (Base64), `algorithm`, `keyVersion`.                                   |
| `SignatureService`              | `WeberTech.Licensing/Crypto` | Assinar (só usado pelo Generator, com a privada) / verificar (usado pelo produto, com a pública), RSA-SHA256. |
| `KeyProvider`                   | `WeberTech.Licensing/Crypto` | Carrega a chave pública embutida como `Resource`; no Generator, carrega a privada protegida.           |
| `MachineIdService`               | `WeberTech.Licensing/Services` | Calcula o Machine ID via WMI (CPU, motherboard, disco de sistema) + SHA-256.                          |
| `ActivationRequestService`       | `WeberTech.Licensing/Services` | Monta o JSON do pedido, gera o texto `WTAREQ1:<base64>` para o QR.                                    |
| `QrCodeService`                  | `WeberTech.Licensing/Services` | Renderiza a imagem do QR Code a partir do texto do pedido (QRCoder).                                   |
| `LicenseIssuer`                  | `WeberTech.Licensing/Services` | **Só usado pelo Generator.** Serializa `License`, assina, grava o envelope `.wta`.                     |
| `LicenseValidator`               | `WeberTech.Licensing/Services` | Valida assinatura + `productId` + `machineId` + expiração; devolve `LicenseStatus`.                    |
| `LicenseStore`                   | `WeberTech.Licensing/Storage`  | Lê/grava o `.wta` em `%ProgramData%\WeberTech\{ProductId}\license.wta`.                                |
| `ProductProfile`                 | `WeberTech.Licensing/Models`   | Lista de `AvailableFeatures`/`AvailablePlans` por `ProductType`, consumida pelo Generator para validar combinações. |
| `Licensing` (fachada estática)   | `WeberTech.Licensing`          | Ponto único de integração para os produtos-cliente (ver Secção 10).                                    |
| `WeberTechLicenseGate`           | **Fora** deste projeto (ex.: `ScoolManager.Desktop/Infrastructure`) | Implementa a interface `ILicenseGate` de cada `*.Core`, chamando `Licensing.*` — a fronteira entre o domínio do produto e este SDK. |

---

## 3. Fase 1 — Setup do Projeto

1. Criar `WeberTech.Licensing.sln` com os três projetos (`WeberTech.Licensing`, `WeberTech.LicenseGenerator`, `WeberTech.Licensing.Tests`).
2. `WeberTech.Licensing.csproj` → `<TargetFramework>net10.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`. **Sem** referência a Avalonia — é regra de arquitetura (Secção 0.2).
3. `GlobalUsings.cs`:
   ```csharp
   global using System;
   global using System.Security.Cryptography;
   global using System.Text;
   global using System.Text.Json;
   global using System.Threading.Tasks;
   ```
4. Pacotes NuGet do Core:
   - `QRCoder` (geração de QR)
   - `System.Management` (consultas WMI para o Machine ID)
5. Pacotes do `WeberTech.LicenseGenerator`:
   - `Avalonia` + `Avalonia.Desktop`
   - `ZXing.Net` (leitura de QR por webcam/imagem)
   - `Microsoft.EntityFrameworkCore.Sqlite` (histórico local de emissões)
   - Referência de projeto a `WeberTech.Licensing`
6. `WeberTech.Licensing.Tests` (xUnit), referenciando o Core.
7. **Não** criar ainda nenhuma referência a partir de `SchoolManager`/`KiVenda`/`SmartGest` — isso só acontece na Fase 8 (integração), depois de o SDK estar publicado (mesmo que localmente, via `ProjectReference` temporária).

---

## 4. Fase 2 — Criptografia (Par de Chaves RSA)

1. Gerar o par de chaves definitivo, **uma única vez, offline**, fora de qualquer projeto versionado em Git:
   ```csharp
   using var rsa = RSA.Create(3072); // mínimo 2048; 3072 é a escolha adotada

   File.WriteAllText("wt_private.pem", rsa.ExportPkcs8PrivateKeyPem()); // NUNCA sai da Weber Tech
   File.WriteAllText("wt_public.pem",  rsa.ExportSubjectPublicKeyInfoPem()); // vai embutida em cada produto
   ```
2. Guardar `wt_private.pem` protegida (PEM cifrado por password, ou Windows DPAPI) — nunca em texto simples, nunca no repositório do `LicenseGenerator`.
3. Embutir `wt_public.pem` como `EmbeddedResource` dentro de `WeberTech.Licensing.csproj`.
4. Implementar `Crypto/KeyProvider.cs`:
   - `RSA LoadPublicKey()` — lê o resource embutido, usado pelo lado do produto-cliente.
   - `RSA LoadPrivateKey(string protectedPath)` — só chamado dentro do `LicenseGenerator`.
5. Implementar `Crypto/SignatureService.cs`:
   ```csharp
   public byte[] Sign(string payloadJson, RSA privateKey) =>
       privateKey.SignData(Encoding.UTF8.GetBytes(payloadJson),
           HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

   public bool Verify(string payloadJson, byte[] signature, RSA publicKey) =>
       publicKey.VerifyData(Encoding.UTF8.GetBytes(payloadJson), signature,
           HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
   ```
6. Teste manual único desta fase: assinar um payload de exemplo com a privada e confirmar que `Verify` com a pública devolve `true`, e `false` se um único caractere do payload for alterado.

---

## 5. Fase 3 — Machine ID

1. Implementar `Services/MachineIdService.cs`, combinando três valores WMI:
   - `Win32_Processor.ProcessorId`
   - `Win32_BaseBoard.SerialNumber`
   - `Win32_DiskDrive.SerialNumber` (disco de sistema)
   ```csharp
   public string GetMachineId()
   {
       string cpuId   = QueryWmi("Win32_Processor", "ProcessorId");
       string boardId = QueryWmi("Win32_BaseBoard", "SerialNumber");
       string diskId  = QueryWmi("Win32_DiskDrive", "SerialNumber");

       string raw = $"{cpuId}|{boardId}|{diskId}";
       byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
       return Convert.ToHexString(hash)[..32];
   }
   ```
2. Corre inteiramente no PC do cliente, sem chamada externa.
3. Documentar explicitamente (código + README) a regra de tolerância: trocar disco externo/impressora não afeta o ID; trocar a motherboard gera novo ID e exige nova ativação.
4. Testes: `MachineIdServiceTests` usa valores WMI mockados/fixos para garantir determinismo (mesmo input → mesmo hash sempre).

---

## 6. Fase 4 — Pedido de Ativação e QR Code

1. `Entities/ActivationRequest.cs`:
   ```csharp
   public class ActivationRequest
   {
       public string Fmt { get; set; } = "WTAREQ1";
       public string ProductId { get; set; }
       public string MachineId { get; set; }
       public Guid RequestId { get; set; }
       public DateTime RequestedAt { get; set; }
   }
   ```
2. `Services/ActivationRequestService.cs`:
   ```csharp
   public string BuildQrText(string productId, string machineId)
   {
       var request = new ActivationRequest
       {
           ProductId = productId,
           MachineId = machineId,
           RequestId = Guid.NewGuid(),
           RequestedAt = DateTime.UtcNow
       };
       string json = JsonSerializer.Serialize(request);
       string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
       return $"WTAREQ1:{base64}";
   }

   public ActivationRequest ParseQrText(string qrText) { /* remove prefixo, decodifica Base64, desserializa */ }
   ```
3. `Services/QrCodeService.cs` — usa `QRCoder` para converter o texto `WTAREQ1:<base64>` numa imagem (bitmap/SVG) a mostrar em ecrã.
4. Lado do Generator: leitura do QR por webcam/imagem via `ZXing.Net` (Fase 7), ou colagem manual do texto Base64 — o segundo caminho é o principal no dia a dia e não depende de câmara.
5. Testes: `ActivationRequestServiceTests` garante *round-trip* (`BuildQrText` → `ParseQrText` devolve os mesmos campos).

---

## 7. Fase 5 — Emissão e Validação do `.wta`

### 7.1 Entidades
```csharp
public class License
{
    public Guid LicenseId { get; set; }
    public string ProductId { get; set; }
    public string CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string MachineId { get; set; }
    public string Plan { get; set; }
    public LicenseType Type { get; set; }
    public string[] Features { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }   // null = perpétua
}

public class LicenseFile   // envelope gravado como .wta
{
    public string Payload { get; set; }     // License serializado, em Base64
    public string Signature { get; set; }   // assinatura RSA, em Base64
    public string Algorithm { get; set; } = "RSA-SHA256";
    public int KeyVersion { get; set; } = 1;
}
```

### 7.2 `LicenseIssuer` (só usado pelo Generator)
```csharp
public LicenseFile Issue(License license, RSA privateKey)
{
    string payloadJson = JsonSerializer.Serialize(license);
    string payloadB64  = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));
    byte[] signature   = _signatureService.Sign(payloadJson, privateKey);

    return new LicenseFile
    {
        Payload = payloadB64,
        Signature = Convert.ToBase64String(signature),
        Algorithm = "RSA-SHA256",
        KeyVersion = 1
    };
}
```

### 7.3 `LicenseValidator` (usado pelo produto-cliente)
```csharp
public LicenseStatus Validate(LicenseFile envelope, string currentProductId, string currentMachineId, RSA publicKey)
{
    byte[] payloadBytes = Convert.FromBase64String(envelope.Payload);
    byte[] signature    = Convert.FromBase64String(envelope.Signature);
    string payloadJson  = Encoding.UTF8.GetString(payloadBytes);

    if (!_signatureService.Verify(payloadJson, signature, publicKey))
        return LicenseStatus.Invalid;

    var license = JsonSerializer.Deserialize<License>(payloadJson);

    if (license.ProductId != currentProductId) return LicenseStatus.ProductMismatch;
    if (license.MachineId != currentMachineId) return LicenseStatus.MachineMismatch;
    if (license.ExpiresAt is { } exp && exp < DateTime.UtcNow) return LicenseStatus.Expired;

    return LicenseStatus.Valid;
}
```

### 7.4 `LicenseStore`
- `Save(LicenseFile, path)` / `Load(path) : LicenseFile?`.
- Caminho padrão: `%ProgramData%\WeberTech\{ProductId}\license.wta` (fora da pasta de instalação, sobrevive a reinstalações/atualizações).
- Guarda também um **carimbo da última verificação bem-sucedida** (mitigação de "recuar o relógio", Secção 9.3) num ficheiro auxiliar ao lado do `.wta`.

### 7.5 Testes
- `LicenseIssuerTests` — emitir e reler produz o mesmo `License`.
- `LicenseValidatorTests` — cobre os 5 cenários de `LicenseStatus` (`Valid`, `Invalid` por payload alterado, `ProductMismatch`, `MachineMismatch`, `Expired`), mais `NotFound` quando não há `.wta` no `LicenseStore`.

---

## 8. Fase 6 — `WeberTech.LicenseGenerator` (Avalonia)

Fluxo em três passos, conforme a documentação (Secção 11 do PDF):

1. **`ActivationView`** — colar o Base64 do QR (caixa de texto) *ou* ler via webcam/imagem (`QrReaderService` com ZXing.Net); decodifica `productId` + `machineId` automaticamente via `ActivationRequestService.ParseQrText`.
2. **`IssueLicenseView`** — formulário com:
   - Seleção do produto (`ProductType`) → filtra `ProductProfile` correspondente, atualizando dinamicamente a lista de módulos/planos disponíveis (impede combinação inválida).
   - Nome do cliente, `MachineId` (preenchido automaticamente, read-only).
   - Plano (dropdown conforme `ProductProfile.AvailablePlans`).
   - Tipo: Assinatura / Perpétua / Trial (`LicenseType`).
   - Datas de início/fim (fim desabilitado se `Perpetual`).
   - Checkboxes de módulos (`ProductProfile.AvailableFeatures`).
3. **Emissão** — botão "Gerar Licença .wta" chama `LicenseIssuer.Issue(...)` com a chave privada carregada via `KeyProvider.LoadPrivateKey`, grava o ficheiro (`Nome_Cliente_Produto.wta`) para envio manual (email/WhatsApp/pen).
4. **`HistoryView`** — lista licenças já emitidas (SQLite local, `EmissionHistoryDbContext`), permitindo reemitir/renovar sem repetir o QR Code inteiro. Guardar pelo menos: `licenseId`, `productId`, `customerName`, `machineId`, `plan`, `issuedAt`, `expiresAt`.
5. `Services/FileDialogService.cs` — encapsula os diálogos nativos de "guardar como" para o `.wta`.

---

## 9. Fase 7 — API Pública (`Licensing`, fachada estática)

Ponto único de integração, exposto pelo Core a qualquer produto-cliente:

```csharp
public static class Licensing
{
    public static void Initialize(ProductType productType, string productId);
    public static LicenseStatus CurrentStatus { get; }
    public static Bitmap GenerateActivationQrCode();
    public static LicenseStatus ImportLicenseFile(string path);
    public static bool HasFeature(string feature);
    public static int DaysUntilExpiration();          // -1 se perpétua
    public static LicenseInfo GetLicenseInfo();
}
```

| Membro                          | Comportamento interno                                                                 |
| -------------------------------- | ---------------------------------------------------------------------------------------- |
| `Initialize(productType, productId)` | Carrega a chave pública embutida (`KeyProvider`), tenta ler o `.wta` local (`LicenseStore`) e valida (`LicenseValidator`); guarda `CurrentStatus` em memória. |
| `CurrentStatus`                  | Getter simples do estado calculado no `Initialize` (ou recalculado a cada leitura, a decidir na implementação). |
| `GenerateActivationQrCode()`     | Usa `MachineIdService` + `ActivationRequestService` + `QrCodeService`.                    |
| `ImportLicenseFile(path)`        | Lê o `.wta` escolhido pelo utilizador, valida, e se `Valid`, grava-o via `LicenseStore` no caminho padrão. |
| `HasFeature(feature)`            | `false` se `CurrentStatus != Valid`; caso contrário, verifica `License.Features`.         |
| `DaysUntilExpiration()`          | `(ExpiresAt - DateTime.UtcNow).Days`, ou `-1` se `ExpiresAt == null`.                      |
| `GetLicenseInfo()`               | Projeta `License` num DTO simples (`LicenseInfo`) para exibição na UI do produto (cliente, plano, validade) — sem expor a entidade interna diretamente. |

Exemplo de uso num produto-cliente:
```csharp
Licensing.Initialize(ProductType.KiVenda, "kivenda.desktop_v03");

if (Licensing.CurrentStatus != LicenseStatus.Valid)
{
    var qr = Licensing.GenerateActivationQrCode();
    ShowActivationScreen(qr);
    return;
}

if (Licensing.HasFeature("Relatorios"))
    HabilitarMenuRelatorios();

int diasRestantes = Licensing.DaysUntilExpiration();
```

---

## 10. Fase 8 — Integração no Software do Cliente

Três pontos de contacto, replicados em cada produto (School Manager primeiro, depois KiVenda e SmartGest — ou KiVenda como piloto, conforme Secção 16 do PDF):

1. **Arranque** — `Licensing.Initialize(...)` antes de mostrar a janela principal.
2. **Ecrã de bloqueio** — se `CurrentStatus != Valid`, mostrar janela com o QR Code + botão "Já tenho um ficheiro de licença" (chama `ImportLicenseFile`) + explicação curta do processo.
3. **Controlo de funcionalidades** — nos pontos onde um módulo pago é usado, verificar `Licensing.HasFeature("X")` antes de habilitar o menu/ecrã.

No caso do `ScoolManager.Core` (ver documento irmão), a integração passa pela interface `ILicenseGate` já definida no domínio:
```csharp
public interface ILicenseGate
{
    bool IsLicenseValid { get; }
    bool HasFeature(string feature);
}
```
implementada em `ScoolManager.Desktop/Infrastructure/WeberTechLicenseGate.cs`, delegando para `Licensing.*`. Este é o padrão a repetir para `KiVenda.Core` e `SmartGest.Core`: **o Core de cada produto nunca referencia `WeberTech.Licensing` diretamente** — só o host (camada Desktop/API) o faz, através da sua própria abstração.

`ProductProfile` de referência (Secção 9 do PDF):

| Produto        | `productId` (exemplo)        | Módulos disponíveis                                  |
| --------------- | ------------------------------ | ------------------------------------------------------- |
| School Manager  | `schoolmanager.desktop_v01`    | Alunos, Propinas, Financeiro, Relatórios                |
| SmartGest       | `smartgest.desktop_v01`        | Contabilidade, IVA/Impostos, Inventário, Relatórios     |
| KiVenda         | `kivenda.desktop_v03`          | Caixa, Estoque, Compras, Vendas                          |

---

## 11. Fase 9 — Segurança, Ameaças e Mitigações

Checklist a validar antes de qualquer emissão real de licença:

| Ameaça                                            | Mitigação implementada                                                                                  | Onde                              |
| --------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ------------------------------------ |
| Copiar o `.wta` para outro PC                       | Assinatura cobre `machineId`; noutra máquina o `MachineId` calculado não bate → `MachineMismatch`.           | `LicenseValidator`                    |
| Editar o payload à mão (mudar `expiresAt`)          | Qualquer alteração ao payload invalida a assinatura RSA.                                                     | `SignatureService.Verify`             |
| Recuar o relógio do sistema para "renovar" um trial | Carimbo da última verificação bem-sucedida gravado junto ao `.wta`; hora atual anterior a esse carimbo = adulteração assumida. | `LicenseStore`                        |
| Extrair a chave pública e gerar licenças falsas     | A pública só verifica, nunca assina; sem a privada não há assinatura válida possível.                        | Design de chaves (Fase 2)             |
| Decompilar o software e "saltar" a verificação      | Ofuscação do binário (ConfuserEx / .NET Reactor) + verificação em múltiplos pontos do código, não só no arranque. | Build/release dos produtos-cliente    |
| Reutilizar o mesmo `requestId` de ativação          | Histórico de `requestId` já emitidos guardado no `LicenseGenerator` (opcional para v1; mais relevante com revogação online futura). | `HistoryView` / `EmissionHistoryDbContext` |

**Nota sobre revogação:** sistema 100% offline não permite desligar remotamente uma licença já entregue. Isto é uma limitação consciente do modelo v1 e deve constar da documentação de venda/suporte; resolução futura via `WeberTech.Licensing.Api` (Secção 12).

---

## 12. Fase 10 — Testes

- `SignatureServiceTests` — assinar e verificar com o mesmo par de chaves funciona; verificar com payload alterado falha; verificar com par de chaves diferente falha.
- `MachineIdServiceTests` — mesmo input WMI (mockado) → mesmo hash, sempre; inputs diferentes → hashes diferentes.
- `ActivationRequestServiceTests` — *round-trip* `BuildQrText`/`ParseQrText`; prefixo `WTAREQ1:` obrigatório na leitura.
- `LicenseIssuerTests` — `License` emitido e relido preserva todos os campos, incluindo `Features` e `ExpiresAt` nulo (perpétua).
- `LicenseValidatorTests` — cobre `Valid`, `Invalid`, `ProductMismatch`, `MachineMismatch`, `Expired`, `NotFound`.
- Testes de integração do `LicenseGenerator` ficam fora do escopo do `WeberTech.Licensing.Tests` (dependem de Avalonia) — validação manual do fluxo ponta-a-ponta na Fase 8/piloto.

---

## 13. Fase 11 — Empacotamento e Distribuição

1. Publicar `WeberTech.Licensing` num feed NuGet **privado** (Azure Artifacts, GitHub Packages, ou feed próprio) — nunca no NuGet.org público, porque a lógica de validação e a chave pública embutida são específicas da Weber Tech.
2. Cada novo produto/cliente-parceiro recebe o seu próprio `productId` e é registado como um novo `ProductProfile` no `LicenseGenerator`, **sem** precisar recompilar o Core.
3. Documentação de integração reduzida a: instalar o pacote → chamar `Licensing.Initialize` → tratar o ecrã de ativação (Secção 10 deste roteiro).
4. Reservar os nomes de projeto para a extensibilidade futura, sem os criar ainda: `WeberTech.Licensing.Api` (emissão remota + lista de revogação), `WeberTech.Licensing.CLI` (emissão em lote), `WeberTech.Licensing.SDK` (pacote público/semi-público para terceiros).

---

## 14. Pendências Reais Ainda em Aberto

1. ~~**Escolha final do padding RSA**~~ — **Decidido:** `RSASignaturePadding.Pss`. Fixado em `SignatureService.cs` (constante `AlgorithmIdentifier = "RSA-SHA256-PSS"`, a gravar no campo `algorithm` do envelope `.wta` na Fase 5). Os exemplos de código originais do PDF usam PKCS#1 v1.5, mas a implementação real segue esta decisão — não usar Pkcs1 em nenhum ponto novo do código.
2. **Modo híbrido (validação online opcional)** — mencionado na Secção 15 do PDF como extensão futura; não faz parte do escopo desta primeira versão, mas o formato do `.wta` já deve ser tratado como estável para não quebrar esse caminho depois.
3. **Rotação de chaves** — o campo `keyVersion` no envelope já existe para isso, mas o mecanismo de "quais versões de chave pública o validador aceita" ainda não está desenhado; só entra em jogo quando existir uma segunda geração de chaves.

Tudo o resto (formato do `.wta`, fluxo de ativação por QR, três pontos de integração no cliente, `ProductProfile` por produto) está **decidido e fechado** pela documentação v1.0.

---

## 15. Ordem de Execução

```
Fase 1  (Setup: solução + 3 projetos + pacotes)
  → Fase 2  (RSA: geração do par de chaves, SignatureService, KeyProvider)
  → Fase 3  (MachineIdService)
  → Fase 4  (ActivationRequest + QrCodeService)
  → Fase 5  (License, LicenseFile, LicenseIssuer, LicenseValidator, LicenseStore)
  → Fase 6  (WeberTech.LicenseGenerator em Avalonia)
  → Fase 7  (Fachada estática Licensing — API pública)
  → Fase 8  (Piloto: integração num produto real, ex.: KiVenda ou School Manager)
  → Fase 9  (Checklist de segurança)
  → Fase 10 (Testes)
  → Fase 11 (Empacotamento NuGet privado)
```

Pronto para gerar código real a partir daqui.
