# Oficina Tech — Sistema de Gestão de Ordens de Serviço

## What This Is

Backend MVP para uma oficina mecânica de médio porte, construído em C# + .NET com arquitetura monolítica em camadas (DDD-inspired). O sistema gerencia o ciclo completo das Ordens de Serviço (OS): criação, acompanhamento de status, aprovação de orçamentos pelo cliente, controle de peças/estoque e gestão administrativa. Projeto acadêmico individual — FIAP POS TECH Tech Challenge Fase 1.

## Core Value

O cliente consegue abrir uma OS, receber um orçamento, aprovar serviços adicionais via API e acompanhar o status em tempo real — do recebimento à entrega.

## Business Context

- **Customer**: Avaliadores FIAP + simulação de oficina mecânica
- **Revenue model**: Projeto acadêmico (nota = 90% da fase)
- **Success metric**: Todos os requisitos obrigatórios entregues com 80%+ de cobertura de testes nos domínios críticos
- **Deadline**: 2026-09-01

## Requirements

### Validated

(None yet — ship to validate)

### Active

**Domínio: Clientes**
- [ ] CRUD-CLI-01: Usuário admin pode criar, ler, atualizar e excluir clientes (pessoa física e jurídica)
- [ ] CRUD-CLI-02: CPF e CNPJ são validados no cadastro e atualizações
- [ ] CRUD-CLI-03: Cliente pode consultar o status de suas OS via API pública (sem autenticação administrativa)

**Domínio: Veículos**
- [ ] CRUD-VEI-01: Usuário admin pode cadastrar veículos vinculados a um cliente (placa, marca, modelo, ano)
- [ ] CRUD-VEI-02: Placa do veículo é validada no cadastro
- [ ] CRUD-VEI-03: Usuário admin pode listar e detalhar veículos

**Domínio: Serviços**
- [ ] CRUD-SRV-01: Usuário admin pode criar, ler, atualizar e excluir tipos de serviço (ex: troca de óleo, alinhamento) com preço
- [ ] CRUD-SRV-02: Sistema monitora e expõe tempo médio de execução por tipo de serviço

**Domínio: Peças e Insumos**
- [ ] CRUD-PEC-01: Usuário admin pode criar, ler, atualizar e excluir peças e insumos com controle de estoque
- [ ] CRUD-PEC-02: Estoque é atualizado automaticamente ao vincular peças a uma OS aprovada

**Domínio: Ordem de Serviço (OS)**
- [ ] OS-01: Criação de OS identifica cliente por CPF/CNPJ e associa veículo cadastrado
- [ ] OS-02: OS aceita múltiplos serviços solicitados e peças/insumos necessários
- [ ] OS-03: Orçamento gerado automaticamente com base nos serviços e peças vinculados
- [ ] OS-04: Orçamento enviado ao cliente para aprovação (endpoint de aprovação)
- [ ] OS-05: OS percorre os 6 status em ordem: Recebida → Em diagnóstico → Aguardando aprovação → Em execução → Finalizada → Entregue
- [ ] OS-06: Transições de status são disparadas automaticamente pelas ações do sistema
- [ ] OS-07: Listagem e detalhamento de OS disponíveis para admin

**Segurança**
- [ ] SEC-01: APIs administrativas protegidas por autenticação JWT
- [ ] SEC-02: Dados sensíveis (CPF/CNPJ, placa) validados em todas as entradas

**Qualidade e Infraestrutura**
- [ ] QA-01: Testes unitários e de integração com cobertura mínima de 80% nos domínios críticos (OS, Clientes, Peças)
- [ ] INFRA-01: APIs RESTful documentadas via Swagger/OpenAPI
- [ ] INFRA-02: Dockerfile para build da aplicação
- [ ] INFRA-03: docker-compose.yml orquestrando app + banco
- [ ] INFRA-04: README.md com instruções de uso, setup local e objetivos

### Out of Scope

- Frontend / app mobile — backend-only conforme especificação
- Notificações push/email — não mencionado nos requisitos; backend de API apenas
- Multi-tenancy / múltiplas oficinas — MVP de oficina única
- OAuth / SSO — JWT é o requisito explícito
- Pagamentos — fora do escopo do MVP
- Dashboard analítico — além do tempo médio de execução já previsto

## Context

- **Acadêmico**: FIAP POS TECH, Fase 1, Tech Challenge — atividade obrigatória valendo 90% da nota
- **Trabalho individual**: projeto desenvolvido solo, sem equipe
- **Prazo**: 2026-09-01 (~9 dias a partir do início)
- **Entregáveis além do código**: vídeo demo (até 15 min), documentação DDD no Miro (Event Storming), relatório de análise de vulnerabilidades, documento PDF de entrega
- **Repositório**: privado, com acesso ao usuário `soatarchitecture`

## Constraints

- **Tech stack**: C# + .NET — monolítico com arquitetura em camadas (Domain, Application, Infrastructure, API)
- **Database**: PostgreSQL — escolha justificada pela natureza relacional dos dados (OS tem vínculos fortes entre clientes, veículos, serviços e peças; transações garantem consistência do estoque)
- **Coverage**: Mínimo 80% de cobertura em domínios críticos (OS, Clientes, Peças)
- **Timeline**: 9 dias — sem espaço para arquitetura especulativa; focar no caminho crítico dos requisitos obrigatórios
- **Acesso**: Repositório privado deve incluir usuário `soatarchitecture` com acesso

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| C# + .NET monolito | Requisito acadêmico (monolítico) + stack escolhida pelo dev | — Pending |
| PostgreSQL | Dados altamente relacionais (OS → cliente → veículo → serviços → peças); transações garantem consistência do estoque | — Pending |
| Arquitetura em camadas (Domain / Application / Infrastructure / API) | Alinha com DDD conforme exigido; viável para MVP sem overhead de microserviços | — Pending |
| JWT para autenticação admin | Requisito explícito do desafio | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-08-23 after initialization*
