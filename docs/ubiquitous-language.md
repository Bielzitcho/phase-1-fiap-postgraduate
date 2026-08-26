# Linguagem Ubíqua — Oficina Tech

## Introdução

Este documento define os termos do domínio do negócio (português) e os mapeia para os
identificadores de código (inglês) utilizados na implementação. Os termos foram derivados
diretamente do código-fonte do projeto e validados contra os nomes reais de classes, métodos
e propriedades.

O objetivo é garantir que toda a equipe — desenvolvedores, avaliadores acadêmicos e
stakeholders — compartilhe o mesmo vocabulário ao discutir requisitos, código e regras de
negócio. Qualquer discrepância entre este documento e o código-fonte deve ser tratada como
um defeito.

---

## Contexto Delimitado: Gestão de Ordens de Serviço

### Mapeamento de Termos

| Termo PT (Ubíquo)              | Código C# (EN)                | Tipo                           | Definição                                                                                                                                                                     |
|--------------------------------|-------------------------------|--------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Ordem de Serviço (OS)          | `ServiceOrder`                | Aggregate Root                 | Entidade central que rastreia um serviço de reparo desde a entrada até a entrega. Contém serviços, peças e status. Identificada por `Guid`. Status com setter privado (SC-2). |
| Cliente                        | `Client`                      | Aggregate Root                 | Pessoa física (CPF) ou jurídica (CNPJ) que possui veículos e abre ordens de serviço. Identificada por `Guid`.                                                                |
| CPF / CNPJ                     | `TaxId`                       | Value Object                   | Documento fiscal brasileiro. Validado com algoritmo de dígito verificador da Receita Federal. Armazenado sem formatação (apenas dígitos). Todos os dígitos iguais são rejeitados. |
| Placa                          | `LicensePlate`                | Value Object                   | Placa de veículo nos formatos antigo (ABC-1234) ou Mercosul (ABC1D23). Armazenada normalizada (7 chars, sem hífen). Input em minúsculas é aceito e convertido para maiúsculas. |
| Veículo                        | `Vehicle`                     | Aggregate Root                 | Automóvel identificado por `LicensePlate`, vinculado a um `Client` via `ClientId`.                                                                                            |
| Tipo de Serviço                | `ServiceType`                 | Aggregate Root                 | Categoria de serviço de reparo com preço base e tempo médio de execução calculado. Rastreia duração média via acumuladores privados.                                          |
| Peça / Insumo                  | `Part`                        | Aggregate Root                 | Componente físico com estoque (`StockQuantity`) e token de concorrência otimista (`[ConcurrencyCheck]`). Sujeito a race condition na aprovação de OS.                         |
| Serviço na OS                  | `OrderedService`              | Entity (filho)                 | Snapshot de `ServiceType` adicionado à OS. Preço (`UnitPrice`) congelado no momento da adição; imune a mudanças posteriores no `ServiceType`.                                |
| Peça na OS                     | `OrderedPart`                 | Entity (filho)                 | Snapshot de `Part` (qty + preço) adicionado à OS. Preço (`UnitPrice`) e quantidade (`Quantity`) congelados no momento da adição.                                              |
| Orçamento                      | `TotalAmount`                 | Propriedade Computada          | Soma de todos os `OrderedServices` (preço unitário) mais a soma de (`UnitPrice × Quantity`) de todos os `OrderedParts`. Nunca armazenado; sempre recalculado em tempo real.   |
| Status da OS                   | `ServiceOrderStatus`          | Enum                           | Seis estados ordenados: `Recebida`, `EmDiagnostico`, `AguardandoAprovacao`, `EmExecucao`, `Finalizada`, `Entregue`. Setter privado garante transições apenas via métodos de domínio. |
| Iniciar Diagnóstico            | `StartDiagnosis()`            | Método de Domínio              | Transição `Recebida → EmDiagnostico`. Lança `DomainException` se a OS não estiver em `Recebida`.                                                                             |
| Enviar para Aprovação          | `SendForApproval()`           | Método de Domínio              | Transição `EmDiagnostico → AguardandoAprovacao`. Lança `DomainException` se a OS não estiver em `EmDiagnostico`.                                                             |
| Aprovação de Orçamento         | `Approve()`                   | Método de Domínio              | Transição `AguardandoAprovacao → EmExecucao`. Exige verificação de titularidade pelo `TaxId` do cliente na camada Application. Lança `DomainException` se o status for diferente. |
| Iniciar Execução               | `StartExecution()`            | Método de Domínio (guarda idempotente) | Guarda que valida que a OS já está em `EmExecucao`. Não altera o status; lança `DomainException` se chamado em outro status. Permite que a Application confirme que `Approve()` foi chamado antes.  |
| Finalizar OS                   | `Finalize()`                  | Método de Domínio              | Transição `EmExecucao → Finalizada`. Define `FinalizationDate = DateTime.UtcNow`. A camada Application dispara `RecordExecution()` em cada `ServiceType` relacionado.         |
| Marcar como Entregue           | `MarkDelivered()`             | Método de Domínio              | Transição `Finalizada → Entregue`. Transição terminal; nenhuma modificação posterior é permitida na OS.                                                                       |
| Decremento de Estoque          | `DecrementStock(qty)`         | Método de Domínio em `Part`    | Reduz `StockQuantity` pela quantidade informada. Lança `DomainException` se o estoque for insuficiente ou se `qty <= 0`.                                                      |
| Regra de Domínio               | `DomainException`             | Exceção                        | Classe base para todas as violações de regra de negócio. Não `sealed` para permitir subtipagem. Capturada pelo `DomainExceptionHandler` na camada Presentation → HTTP 400.    |
| Concorrência Otimista          | `ConcurrencyDomainException`  | Subtipo de Exceção (`sealed`)  | Lançada quando `DbUpdateConcurrencyException` é capturada em `EfUnitOfWork` durante atualização de `Part`. Mapeada pelo `DomainExceptionHandler` → HTTP 409 Conflict.         |
| Tempo Médio de Execução        | `AverageExecutionTime`        | Propriedade Computada em `ServiceType` | Média corrente da duração entre criação e finalização da OS, calculada via acumuladores privados `_executionCount` (int) e `_totalExecutionMinutes` (double). Disparada por `RecordExecution()`. |
| Unidade de Trabalho            | `IUnitOfWork`                 | Interface (Application)        | Abstrai o commit de persistência. Implementada por `EfUnitOfWork` na camada Infrastructure. `CommitAsync()` chamado uma única vez por operação de serviço (D-03).              |
| Repositório                    | `IRepository<T>` / `I*Repository` | Interface (Domain)        | Contrato de acesso a dados por agregado. Implementações vivem em Infrastructure; interfaces vivem em Domain (zero dependência externa).                                       |

---

## Convenções de Implementação

- **Setters privados:** Todas as entidades têm setters privados. Mudanças de estado ocorrem
  exclusivamente via métodos de comportamento nomeados.
- **Domain layer zero-dependency:** O projeto `OficinaTech.Domain` não possui referências a
  NuGet packages externos (zero referências a EF Core, ASP.NET Core, etc.).
- **IUnitOfWork na Application:** A interface `IUnitOfWork` está no projeto
  `OficinaTech.Application`, não no Domain. A Infrastructure referencia Application para
  implementar `EfUnitOfWork`.
- **Commit único por operação:** `CommitAsync()` é chamado uma única vez por operação de
  serviço. Repositórios não chamam `SaveChangesAsync()` diretamente.
- **Result pattern:** Operações de serviço retornam `Result<T>` (definido em Seedwork) em vez
  de lançar exceções para fluxos de erro esperados (ex.: entidade não encontrada). Exceções de
  domínio são lançadas apenas para violações de regra de negócio.
- **Concorrência otimista em Part:** O campo `[ConcurrencyCheck]` garante que aprovações
  simultâneas de OS com a mesma peça resultem em `ConcurrencyDomainException` (HTTP 409) para
  a segunda chamada, sem corromper o estoque.

---

## Mapeamento de Endpoints

| Ação de Negócio                   | Endpoint HTTP                                              | Auth     |
|-----------------------------------|-------------------------------------------------------------|----------|
| Login de administrador            | `POST /api/auth/login`                                     | Público  |
| Criar cliente                     | `POST /api/clients`                                        | JWT      |
| Listar clientes (paginado)        | `GET /api/clients`                                         | JWT      |
| Obter cliente por ID              | `GET /api/clients/{id}`                                    | JWT      |
| Atualizar cliente                 | `PUT /api/clients/{id}`                                    | JWT      |
| Remover cliente                   | `DELETE /api/clients/{id}`                                 | JWT      |
| Criar veículo                     | `POST /api/vehicles`                                       | JWT      |
| Listar veículos por cliente       | `GET /api/clients/{clientId}/vehicles`                     | JWT      |
| Obter veículo por ID              | `GET /api/vehicles/{id}`                                   | JWT      |
| Atualizar veículo                 | `PUT /api/vehicles/{id}`                                   | JWT      |
| Remover veículo                   | `DELETE /api/vehicles/{id}`                                | JWT      |
| Criar tipo de serviço             | `POST /api/service-types`                                  | JWT      |
| Listar tipos de serviço           | `GET /api/service-types`                                   | JWT      |
| Obter tipo de serviço por ID      | `GET /api/service-types/{id}`                              | JWT      |
| Atualizar tipo de serviço         | `PUT /api/service-types/{id}`                              | JWT      |
| Remover tipo de serviço           | `DELETE /api/service-types/{id}`                           | JWT      |
| Criar peça                        | `POST /api/parts`                                          | JWT      |
| Listar peças                      | `GET /api/parts`                                           | JWT      |
| Obter peça por ID                 | `GET /api/parts/{id}`                                      | JWT      |
| Atualizar peça                    | `PUT /api/parts/{id}`                                      | JWT      |
| Remover peça                      | `DELETE /api/parts/{id}`                                   | JWT      |
| Criar ordem de serviço            | `POST /api/service-orders`                                 | JWT      |
| Listar ordens de serviço          | `GET /api/service-orders`                                  | JWT      |
| Obter OS por ID                   | `GET /api/service-orders/{id}`                             | JWT      |
| Iniciar diagnóstico (OS)          | `POST /api/service-orders/{id}/start-diagnosis`            | JWT      |
| Enviar OS para aprovação          | `POST /api/service-orders/{id}/send-for-approval`          | JWT      |
| Aprovar orçamento (público)       | `POST /api/service-orders/{id}/approve`                    | Público  |
| Finalizar OS                      | `POST /api/service-orders/{id}/finalize`                   | JWT      |
| Marcar OS como entregue           | `POST /api/service-orders/{id}/mark-delivered`             | JWT      |
| Consultar status da OS (público)  | `GET /api/service-orders/{id}/status`                      | Público  |
| Adicionar serviço à OS            | `POST /api/service-orders/{id}/services`                   | JWT      |
| Adicionar peça à OS               | `POST /api/service-orders/{id}/parts`                      | JWT      |

---

## Diagrama de Estados — ServiceOrder

```
Recebida
  │
  ▼  StartDiagnosis()
EmDiagnostico
  │
  ▼  SendForApproval()
AguardandoAprovacao
  │
  ▼  Approve()  [verifica TaxId do cliente + decrementa estoque de todas as partes]
EmExecucao
  │
  ▼  Finalize()  [registra FinalizationDate + dispara RecordExecution() por ServiceType]
Finalizada
  │
  ▼  MarkDelivered()
Entregue  (estado terminal)
```

> Qualquer transição fora da sequência acima lança `DomainException` com mensagem descritiva.
> Adicionar serviços ou peças à OS é permitido apenas nos estados `Recebida` e `EmDiagnostico`.

---

## Glossário Complementar

| Termo Técnico          | Significado no Contexto do Projeto                                                                                         |
|------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| Aggregate Root         | Entidade raiz que controla o acesso a um cluster de objetos de domínio. Repositórios operam apenas em Aggregate Roots.     |
| Value Object           | Objeto sem identidade própria; dois VOs com os mesmos valores são idênticos (`TaxId`, `LicensePlate`).                      |
| Seedwork               | Pacote interno de classes base reutilizáveis (`Entity<T>`, `AggregateRoot<T>`, `ValueObject`, `DomainException`, `Result`). |
| Result Pattern         | Estrutura que encapsula sucesso (`Result.Success<T>`) ou falha (`Result.Failure<T>`) sem lançar exceções para erros esperados. |
| Concorrência Otimista  | Estratégia em que o banco verifica um token (`[ConcurrencyCheck]`) antes de commitar; conflito gera `ConcurrencyDomainException`. |
| DDD                    | Domain-Driven Design — abordagem de modelagem em que o design do software reflete o modelo de negócio.                      |

---

*Documento gerado em 2026-08-26. Derivado do código-fonte via leitura direta dos agregados,
enums, exceções e controladores do projeto `OficinaTech`.*
