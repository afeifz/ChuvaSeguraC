# ChuvaSegura – Serviço de Monitoramento (C# / .NET)

> **Global Solution FIAP 2026.2 · CitizenTech · Engenharia de Software · C# Software Development**

## Ficha da CitizenTech

| Campo | Conteúdo |
|---|---|
| **Nome da CitizenTech** | ChuvaSegura |
| **Cidade e bairro** | São Paulo – Ipiranga (região do Rio Tamanduateí) |
| **Cidadão beneficiado** | Moradores e comerciantes do Ipiranga em ruas com histórico de alagamento, com atenção a idosos e pessoas com mobilidade reduzida |
| **Problema, em uma frase** | Moradores de áreas que alagam recebem o aviso de chuva forte tarde demais, ou de forma genérica demais, para proteger a família e seus bens. |
| **Como funciona** | Um sensor mede o nível da água num ponto crítico; um modelo de ML estima a chance de chuva forte; a API combina os dois e envia alertas ao app do morador, que também registra ocorrências. |
| **O que cada parte faz** | IoT (ESP32/Wokwi): mede e sinaliza o nível · **API C#: recebe leituras e calcula risco** · API Java: cadastro e alertas · ML: previsão de chuva forte com dados do INMET · App Expo: risco do bairro, alertas e ocorrências · Linux: hospeda e monitora o sistema · Cybersecurity: protege os dados dos moradores · QA: features e casos de teste |
| **Link do repositório** | https://github.com/afeifz/ChuvaSeguraC.git |
| **Indicador de impacto** | Minutos de antecedência entre o alerta e o nível crítico; % de chuvas fortes detectadas pelo modelo (recall) |
| **ODS conectados** | 11 (Cidades sustentáveis), 13 (Ação climática), 9 (Infraestrutura e inovação), 10 (Redução das desigualdades) |

**Integrantes:** 

Mohamed Afif RM 554445

Lucca Cardinale RM 556668

**Dados:** leituras de sensor **simuladas** (Wokwi); pontos de monitoramento de exemplo **fictícios**; nenhum dado pessoal real é usado neste serviço.

---

## O que esta parte faz

Este serviço é o **backend de monitoramento** da ChuvaSegura:

1. **Recebe** a medição do sensor ESP32 (`POST /api/leituras`).
2. **Calcula** o nível da água e **classifica o risco** (Normal, Atenção ou Alerta).
3. **Guarda** o histórico no banco (SQLite via Entity Framework Core).
4. **Avisa** o serviço de Cadastro e Alertas (Java) quando o risco **muda**.
5. **Expõe** o risco por ponto e por bairro para o aplicativo do cidadão.

### Regras de negócio

| Regra | Valor |
|---|---|
| Nível da água | altura de instalação do sensor − distância medida (mínimo 0) |
| Normal | nível < 30 cm |
| Atenção | 30 cm ≤ nível < 60 cm |
| Alerta | nível ≥ 60 cm |
| Faixa válida do sensor (HC-SR04) | distância de 2 a 400 cm; fora disso a leitura é **rejeitada** (HTTP 422), guardada para auditoria e **não altera o risco** |
| Notificação | enviada apenas quando o risco **muda** (evita repetir o mesmo alerta a cada medição) |
| Dado desatualizado | sem leitura há mais de 15 min (configurável) |

Os limites ficam em `appsettings.json` (seção `RegrasRisco`).

---

## Como rodar

**Pré-requisito:** [.NET SDK 8](https://dotnet.microsoft.com/download) ou mais novo.

```bash
# dentro da pasta do projeto
dotnet restore
dotnet build

# subir a API
dotnet run --project src/ChuvaSegura.Monitoramento.Api
```

- Swagger (documentação dos endpoints): **http://localhost:5080/swagger**
- Verificação de saúde: http://localhost:5080/health
- O banco `chuvasegura-monitoramento.db` (SQLite) é criado sozinho na primeira execução, com um ponto de exemplo (`Ponto Ipiranga 01 (simulado)`, id 2).
- Exemplos prontos de requisições: [`requests.http`](requests.http).

### Rodar os testes

```bash
dotnet test
```

São testes unitários (domínio e serviços, com repositórios falsos) e testes de integração (API completa com SQLite temporário).

### Configuração (`src/ChuvaSegura.Monitoramento.Api/appsettings.json`)

| Chave | Para que serve |
|---|---|
| `ConnectionStrings:Default` | Banco SQLite |
| `Seed:Habilitado` | Cria o ponto de exemplo na primeira execução |
| `RegrasRisco:AtencaoCm` / `AlertaCm` | Limites de risco |
| `RegrasRisco:MinutosParaDadoDesatualizado` | Quando o dado é considerado antigo |
| `AlertasService:Habilitado` | Liga a integração com o serviço Java (padrão: desligada, só registra em log) |
| `AlertasService:BaseUrl` / `Caminho` | Endereço do serviço Java |

---

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/leituras` | **Entrada do ESP32.** Registra uma medição: `{ "pontoId": 1, "distanciaCm": 35 }` |
| GET | `/api/pontos` | Lista pontos (filtros `bairro`, `ativo`; paginação `pagina`, `tamanho`) |
| GET | `/api/pontos/{id}` | Detalha um ponto |
| POST | `/api/pontos` | Cadastra um ponto |
| PUT | `/api/pontos/{id}` | Atualiza um ponto |
| DELETE | `/api/pontos/{id}` | Remove o ponto e seu histórico |
| POST | `/api/pontos/{id}/ativar` · `/desativar` | Liga/desliga o recebimento de leituras |
| GET | `/api/pontos/{id}/risco` | Risco atual do ponto, com orientação em linguagem simples |
| GET | `/api/pontos/{id}/leituras` | Histórico paginado (filtros `de`, `ate`, `apenasValidas`) |
| GET | `/api/pontos/{id}/estatisticas?horas=24` | Máximo, média e contagem por risco |
| GET | `/api/bairros/{bairro}/risco` | Maior risco entre os pontos ativos do bairro (usado pelo app) |
| GET | `/health` | Verificação de saúde |

**Códigos de erro (formato `ProblemDetails`):** `400` dados inválidos · `404` não encontrado · `409` conflito (nome duplicado, ponto inativo) · `422` leitura fora da faixa do sensor · `500` erro interno.

### Exemplo: leitura em nível de alerta

```http
POST /api/leituras
Content-Type: application/json

{ "pontoId": 2, "distanciaCm": 35 }
```

```json
{
  "id": 1,
  "pontoId": 2,
  "distanciaCm": 35,
  "nivelAguaCm": 65,
  "risco": "Alerta",
  "valida": true,
  "motivoRejeicao": null,
  "dataHora": "2026-11-05T15:00:00Z"
}
```

---

## Integração com o serviço de Cadastro e Alertas (Java Spring Boot)

Quando o risco de um ponto muda, esta API chama o serviço Java:

```http
POST {AlertasService:BaseUrl}/api/alertas/notificacoes
Content-Type: application/json

{
  "pontoId": 1,
  "bairro": "Ipiranga",
  "risco": "ALERTA",
  "nivelAguaCm": 65.0,
  "dataHoraUtc": "2026-11-05T15:00:00Z"
}
```

`risco` é `NORMAL`, `ATENCAO` ou `ALERTA`. O serviço Java decide o que fazer (por exemplo, não criar alerta quando for `NORMAL`). Se ele estiver fora do ar, a leitura **continua salva** e a falha é apenas registrada em log.

## Contrato com o ESP32 (Wokwi)

O circuito deve enviar, a cada ciclo, um `POST /api/leituras` com `pontoId` e `distanciaCm` (leitura do HC-SR04). Obs.: o Wokwi só enxerga endereços públicos; enquanto a API estiver só em `localhost`, use o plano B de imprimir no Serial o mesmo JSON que a API espera.

---

## Arquitetura

```
src/
├── ChuvaSegura.Monitoramento.Domain          Entidades e regras (Ponto, Leitura, classificador de risco)
├── ChuvaSegura.Monitoramento.Application     Casos de uso, DTOs, interfaces (repositórios, notificador)
├── ChuvaSegura.Monitoramento.Infrastructure  EF Core + SQLite, repositórios, cliente HTTP do serviço de alertas
└── ChuvaSegura.Monitoramento.Api             Controllers, Swagger, tratamento de erros, Program.cs
tests/
└── ChuvaSegura.Monitoramento.Tests           xUnit: unitários e integração
docs/
└── (prints com evidencias do projeto executado)
```

Dependências: `Api → Application, Infrastructure` · `Infrastructure → Application → Domain`. O domínio não depende de nada.

### Onde cada conteúdo da disciplina aparece

| Conteúdo | Onde ver no código |
|---|---|
| **POO** (encapsulamento, abstração, polimorfismo) | `Domain/Entities/Ponto.cs` (setters privados, regras dentro da entidade, construtor que valida) · `IClassificadorRisco` com implementação `ClassificadorRiscoPorLimites` · hierarquia de exceções em `Domain/Exceptions` |
| **Interfaces e Injeção de Dependência** | `Application/Abstractions/*` (interfaces) · `Application/DependencyInjection.cs` e `Infrastructure/DependencyInjection.cs` (registro dos serviços) |
| **Web API REST** | `Api/Controllers/*` · códigos HTTP corretos · `ProblemDetails` em `Api/Middleware/GlobalExceptionHandler.cs` · Swagger com comentários XML |
| **EF Core** | `Infrastructure/Persistence/AppDbContext.cs` e `Configurations/*` (mapeamento, índices, FK com cascata, enum como texto) |
| **LINQ** | `Infrastructure/Repositories/LeituraRepository.cs` (`GroupBy`, `Max`, `Average`, `Count`, filtros, paginação) · `PontoRepository.cs` · `Application/Services/RiscoService.cs` (`Select`, `Max`, `Any`) |
| **Async/Await** | Todas as operações de I/O são `async` e propagam `CancellationToken` |
| **Testes unitários** | `tests/.../Domain` e `tests/.../Application` (repositórios e notificador falsos, relógio controlável via `TimeProvider`) |
| **Qualidade do código** | Camadas separadas, nomes claros, validação no domínio, tratamento central de erros, notificação desacoplada e tolerante a falhas |

### Decisões de projeto

- **SQLite** para rodar sem instalar banco. Para trocar por Oracle ou SQL Server, basta trocar o provedor em `Infrastructure/DependencyInjection.cs`.
- O banco é criado com `EnsureCreated`. Se quiserem usar *migrations*, rodem `dotnet ef migrations add Inicial` (projeto `Infrastructure`, projeto de inicialização `Api`) e troquem `EnsureCreatedAsync` por `MigrateAsync` em `DbInicializador`.
- Valores decimais usam `double` porque o SQLite não suporta bem agregações com `decimal`.
- A notificação ocorre **depois** do commit: o dado do sensor nunca é perdido por falha do serviço de alertas.
- Fora do escopo desta entrega (extensões possíveis): reforçar o risco com a previsão do modelo de ML, autenticação por chave nos sensores e novas tentativas automáticas na chamada ao serviço Java.

---


