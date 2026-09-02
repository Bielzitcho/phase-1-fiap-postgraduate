# Oficina Tech — Sistema de Gestão de Ordens de Serviço

API REST backend para gestão de oficinas mecânicas. Construída com .NET 10 e PostgreSQL, o sistema cobre o ciclo de vida completo da ordem de serviço: cadastro de clientes, vinculação de veículos, criação de OS, geração automática de orçamento, aprovação pelo cliente via CPF/CNPJ, dedução de estoque e acompanhamento de status da entrada até a entrega. O código segue uma arquitetura em camadas baseada em Domain-Driven Design. Este é um projeto acadêmico da Pós-Graduação FIAP — Fase 1.

---

## Objetivos

- Gerenciar o ciclo de vida completo da OS (Ordem de Serviço): abertura, diagnóstico, envio para aprovação, finalização e entrega.
- Autenticar mecânicos e administradores via JWT com expiração configurável.
- Prover CRUD administrativo para os quatro agregados principais: Clientes, Veículos, Tipos de Serviço e Peças.
- Gerar orçamentos automáticos a partir dos serviços e peças solicitados.
- Permitir que clientes aprovem ou rejeitem orçamentos usando apenas CPF ou CNPJ (sem necessidade de cadastro).
- Gerenciar estoque: decrementar a quantidade de Peças na finalização com proteção de concorrência otimista.
- Atingir >= 80% de cobertura de testes nos namespaces OficinaTech.Domain e OficinaTech.Application.

---

## Arquitetura

### Estrutura do Projeto

A solução segue uma organização DDD em quatro camadas. Cada camada é um projeto C# separado em `src/`:

| Camada | Projeto | Responsabilidade |
|--------|---------|-----------------|
| Domain | OficinaTech.Domain | Entidades, value objects, agregados, interfaces de repositório, DomainException. Zero dependências externas — sem EF Core, sem ASP.NET Core. |
| Application | OficinaTech.Application | Serviços de casos de uso, DTOs, interface IUnitOfWork. Orquestra objetos de domínio e chama interfaces de repositório. |
| Infrastructure | OficinaTech.Infrastructure | DbContext EF Core, configurações de entidades, implementações de repositório, EfUnitOfWork, migrações, serviço de credenciais JWT. Referencia Application para implementar IUnitOfWork. |
| Presentation | OficinaTech.Presentation | Host ASP.NET Core 10, controllers, middlewares (DomainExceptionHandler, handler global de exceções), registro da Scalar UI, migrações automáticas na inicialização. |

Os testes ficam em `tests/OficinaTech.Tests/` (projeto xUnit). Os testes de integração utilizam Testcontainers para subir um container PostgreSQL real em tempo de execução.

### Stack de Tecnologias

| Componente | Escolha | Observações |
|------------|---------|-------------|
| Runtime | .NET 10 / ASP.NET Core 10 | Target framework net10.0 |
| Banco de Dados | PostgreSQL 16 (Docker) | Gerenciado via EF Core + Npgsql |
| ORM | Entity Framework Core 10.0.4 | Migrações code-first, aplicadas automaticamente na inicialização |
| Autenticação | JWT Bearer | Expiração padrão de 15 min (configurável via `Admin__JwtExpiryMinutes`) |
| Docs da API | Scalar em /scalar | Substitui Swashbuckle (removido no .NET 9+) |
| Mapeamento | Mapster 10 | Licença MIT; mais rápido que AutoMapper |
| Testes | xUnit 2.9.3 + NSubstitute 6.2 + Testcontainers | Testes unitários + integração |
| SAST | Security Code Scan | Executado via `security-scan` (ver seção Segurança); 0 avisos na solução atual |

---

## Pré-requisitos

- **Docker Desktop >= 20.x** (ou Rancher Desktop) — necessário para o caminho de início rápido
- **.NET SDK 10.0.400 ou superior** — necessário para o caminho de configuração manual
  - macOS: `brew install dotnet`
  - Outros: https://dotnet.microsoft.com/download
- **Git**

---

## Início Rápido (Docker Compose — 5 minutos)

Este caminho requer apenas o Docker Desktop. Não é necessário instalar o .NET SDK localmente.

1. Clone o repositório:

   ```bash
   git clone https://github.com/Bielzitcho/phase-1-fiap-postgraduate && cd phase-1-fiap-postgraduate
   ```

2. Defina os segredos necessários e inicie todos os containers:

   ```bash
   docker compose up --build
   ```

   > O serviço `api` requer que `Admin__PasswordHash` e `Admin__JwtSecret` estejam definidos. Para desenvolvimento local, edite `docker-compose.yml` e substitua os valores de placeholder antes de executar. Consulte a referência de variáveis de ambiente abaixo.

3. Aguarde a seguinte linha nos logs do container:

   ```
   Now listening on: http://0.0.0.0:8080
   ```

   As migrações do EF Core são executadas automaticamente na inicialização. Nenhuma etapa manual de migração é necessária.

4. Abra a Scalar UI para navegar e testar todos os endpoints:

   ```
   http://localhost:8080/scalar
   ```

5. Autentique-se — envie uma requisição POST para obter um token JWT:

   ```bash
   curl -s -X POST http://localhost:8080/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@oficina.tech","password":"<sua-senha-admin>"}'
   ```

   O corpo da resposta contém um campo `token`. Copie esse valor.

6. Use o token na Scalar (clique em "Authorize") ou em qualquer cliente HTTP como header Bearer:

   ```
   Authorization: Bearer <token>
   ```

### Referência de Variáveis de Ambiente (docker-compose.yml)

| Variável | Obrigatória | Descrição |
|----------|-------------|-----------|
| `ConnectionStrings__DefaultConnection` | Sim | String de conexão PostgreSQL |
| `Admin__Email` | Sim | E-mail de login do administrador |
| `Admin__PasswordHash` | Sim | Hash BCrypt da senha do admin (custo mínimo 12) |
| `Admin__JwtSecret` | Sim | Chave de assinatura HMAC-SHA256 (mínimo 32 caracteres, verificado na inicialização) |
| `Admin__JwtExpiryMinutes` | Não | Tempo de vida do token em minutos (padrão: 15) |

---

## Configuração Manual (Sem Docker)

Necessário apenas para desenvolvimento local sem containerizar a API.

1. Inicie o PostgreSQL (Docker apenas para o banco de dados):

   ```bash
   docker compose up -d postgres
   ```

2. Exporte a string de conexão:

   ```bash
   export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=oficina_tech;Username=oficina;Password=oficina_secret"
   ```

3. Instale a ferramenta CLI do EF Core (se ainda não estiver instalada):

   ```bash
   dotnet tool restore
   ```

4. Aplique as migrações:

   ```bash
   dotnet ef database update \
     --project src/OficinaTech.Infrastructure \
     --startup-project src/OficinaTech.Infrastructure
   ```

5. Execute a API:

   ```bash
   dotnet run --project src/OficinaTech.Presentation
   ```

6. A API estará disponível na porta exibida no console (geralmente `http://localhost:5000`).

---

## Referência da API

Todos os endpoints também estão documentados de forma interativa em `http://localhost:8080/scalar`.

### Autenticação

| Método | Caminho | Descrição |
|--------|---------|-----------|
| POST | /api/auth/login | Body: `{"email":"…","password":"…"}` — Retorna `{"token":"…"}` |

### Endpoints Administrativos (JWT obrigatório)

Todos os endpoints administrativos requerem o header `Authorization: Bearer <token>`.

| Recurso | Caminho Base | Operações Principais |
|---------|-------------|---------------------|
| Clientes | /api/clients | GET (lista, filtro por taxId), GET /{id}, POST, PUT /{id}, DELETE /{id} |
| Veículos | /api/vehicles | GET (lista), GET /{id}, POST, PUT /{id}, DELETE /{id} |
| Veículos por cliente | /api/clients/{clientId}/vehicles | GET — lista veículos de um cliente específico |
| Tipos de Serviço | /api/service-types | GET (lista), GET /{id}, POST, PUT /{id}, DELETE /{id} |
| Peças | /api/parts | GET (lista), GET /{id}, POST, PUT /{id}, DELETE /{id} |
| Ordens de Serviço | /api/service-orders | GET (lista), GET /{id}, POST, PUT /{id} |

Endpoints de transição de status da OS:

| Método | Caminho | Descrição |
|--------|---------|-----------|
| POST | /api/service-orders/{id}/start-diagnosis | Move a OS de Recebida para EmDiagnostico |
| POST | /api/service-orders/{id}/send-for-approval | Move a OS de EmDiagnostico para AguardandoAprovacao |
| POST | /api/service-orders/{id}/approve | (Rota admin) Aprova forçadamente uma OS |
| POST | /api/service-orders/{id}/finalize | Move a OS aprovada para Finalizada; decrementa estoque |
| POST | /api/service-orders/{id}/mark-delivered | Move a OS finalizada para Entregue |

### Endpoints Públicos (sem autenticação)

| Método | Caminho | Descrição |
|--------|---------|-----------|
| POST | /api/service-orders/{id}/approve | Cliente aprova (ou rejeita) um orçamento usando CPF/CNPJ — body: `{"taxId":"…","approved":true}` |
| GET | /api/service-orders/by-client?taxId={taxId} | Cliente consulta sua lista de OS por CPF ou CNPJ |

---

## Executando os Testes

```bash
# Apenas testes unitários (rápido — não requer Docker)
dotnet test tests/OficinaTech.Tests/OficinaTech.Tests.csproj

# Testes unitários com relatório de cobertura (filtrado por namespace)
dotnet test tests/OficinaTech.Tests/OficinaTech.Tests.csproj \
  --settings coverlet.runsettings \
  --results-directory TestResults/

# Suite completa incluindo testes de integração (requer Docker Desktop em execução)
dotnet test tests/OficinaTech.Tests/OficinaTech.Tests.csproj
```

Cobertura atual: >= 80% nos namespaces OficinaTech.Domain e OficinaTech.Application.

Os relatórios de cobertura são gravados em `TestResults/` no formato Cobertura XML. Abra `TestResults/coverage.cobertura.xml` com qualquer visualizador compatível com Cobertura (ex: ReportGenerator).

---

## Segurança

- O segredo JWT é configurado via `Admin__JwtSecret` (mínimo de 32 caracteres verificado na inicialização; a aplicação falha ao iniciar se for menor).
- A senha do admin é armazenada como hash BCrypt (`Admin__PasswordHash`); a senha em texto puro nunca é persistida.
- O endpoint de login retorna um erro genérico tanto para e-mail quanto para senha incorretos, prevenindo enumeração de contas.
- Varredura SAST via Security Code Scan. A ferramenta (`security-scan` 5.6.7) é compilada para .NET 6; em máquinas apenas com runtimes mais novos, defina `DOTNET_ROLL_FORWARD=LatestMajor` para que ela rode sobre o .NET instalado:

  ```bash
  # Linux/macOS
  DOTNET_ROLL_FORWARD=LatestMajor security-scan OficinaTech.sln --export docs/security-report.sarif
  ```

  ```powershell
  # Windows (PowerShell)
  $env:DOTNET_ROLL_FORWARD = "LatestMajor"; security-scan OficinaTech.sln --export docs/security-report.sarif
  ```

- Varredura de CVEs em dependências:

  ```bash
  dotnet list package --vulnerable --include-transitive
  ```

- Resultado atual: 0 críticos, 0 CVEs. Consulte `docs/security-report.sarif` e `docs/vulnerability-report.txt`.

---

## Glossário do Domínio (Linguagem Ubíqua)

| Termo (PT) | Termo (EN) | Definição |
|------------|------------|-----------|
| Ordem de Serviço (OS) | Service Order | Agregado central; rastreia todo o fluxo de reparo de uma visita de veículo |
| Orçamento | Budget | Total calculado automaticamente a partir dos serviços e peças solicitados; enviado ao cliente para aprovação |
| TaxId | TaxId | CPF (pessoa física) ou CNPJ (pessoa jurídica) como value object; validado pelo algoritmo de dígito verificador |
| Placa | License Plate | Value object que aceita os formatos antigo (ABC-1234) e Mercosul (ABC1D23) |
| Aprovação | Approval | Ação do cliente (aprovar ou rejeitar) que desbloqueia ou cancela a execução da OS |
| Peça | Part | Item de inventário com quantidade em estoque; decrementado na finalização da OS |
| Tipo de Serviço | Service Type | Serviço nomeado com preço e rastreador de tempo médio de execução |

---

## Grupo e Entrega (FIAP)

- **Repositório:** https://github.com/Bielzitcho/phase-1-fiap-postgraduate
- **Diagramas C4 (Excalidraw):** https://excalidraw.com/#json=D1HIPRYUUIh4oYgBQ5QFf,M6YFtZdQGnkw1_VWbvbVCQ
- **Event Storming (Miro):** https://miro.com/app/board/uXjVHsplP2M=/?share_link_id=834450438055
- **Grupo:** [nome do grupo] | **Participantes:** [lista] | **Discord:** [usernames] | **Vídeo:** [link]

> Esta seção deve ser completada pelo grupo antes da submissão do PDF.
