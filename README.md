# CreditoSimulado

API REST para simulação e análise de propostas de crédito desenvolvida em C# e .NET 10.

Projeto criado para estudo e demonstração prática de Clean Architecture, CQRS, DDD, SOLID, PostgreSQL, APIs REST e testes automatizados.

> Este sistema não concede crédito real. As regras e decisões são exclusivamente para simulação.

## Tecnologias

- C# / .NET 10
- ASP.NET Core
- PostgreSQL 17
- Npgsql
- Docker Compose
- Clean Architecture
- CQRS
- DDD
- SOLID
- REST
- OpenAPI / Scalar
- ProblemDetails
- xUnit
- Testes unitários e de integração

## Arquitetura

A solução é dividida em:

- `Credito.Domain`: modelo e regras de negócio.
- `Credito.Application`: Commands, Queries, Handlers e abstrações.
- `Credito.Infrastructure`: persistência PostgreSQL com Npgsql.
- `Credito.Api`: API REST e composição das dependências.
- `Credito.Tests`: testes unitários.
- `Credito.IntegrationTests`: testes de integração com PostgreSQL.

Fluxo principal:

    HTTP
      |
      v
    API
      |
      v
    Application
      |
      v
    Domain
      |
      v
    IProposalRepository
      |
      v
    PostgreSQL

## Regras de crédito

A proposta contém referência do cliente, valor solicitado, prazo e renda mensal declarada.

A prestação utiliza Tabela Price com taxa simulada de 1,5% ao mês.

- comprometimento menor que 25%: Approved
- entre 25% e 30%: ManualReview
- acima de 30%: Rejected

Uma proposta já analisada mantém sua decisão.

## API

Endpoints disponíveis:

    GET  /health
    POST /api/proposals
    GET  /api/proposals/{id}
    POST /api/proposals/{id}/analyze

A API utiliza ProblemDetails para padronização dos erros HTTP.

## PostgreSQL

O PostgreSQL é executado localmente através de Docker Compose.

Credenciais exclusivamente para desenvolvimento:

    Database: credito
    Username: credito
    Password: credito_local
    Port: 5432

Inicie o ambiente:

    .\scripts\setup-local.ps1

Configure a connection string no PowerShell:

    $env:ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=credito;Username=credito;Password=credito_local"

Execute:

    dotnet run --project .\src\Credito.Api\Credito.Api.csproj

API:

    http://localhost:5000

Scalar:

    http://localhost:5000/scalar/v1

OpenAPI:

    http://localhost:5000/openapi/v1.json

## Testes

Configure o banco de integração:

    $env:CREDITO_TEST_POSTGRES = "Host=localhost;Port=5432;Database=credito;Username=credito;Password=credito_local"

Execute:

    dotnet test .\CreditoSimuladoLite.sln

## Build

    dotnet build .\CreditoSimuladoLite.sln

## Git Flow

A evolução do projeto utiliza branches de feature, por exemplo:

    main
      |
      +-- feature/ddd-domain-model
      +-- feature/rabbitmq-events
      +-- feature/http-integration-tests

## Próximas evoluções

- aprofundar o modelo DDD;
- melhorar os contratos OpenAPI;
- logging estruturado;
- health check do PostgreSQL;
- testes HTTP/end-to-end;
- RabbitMQ;
- eventos de domínio;
- Worker para processamento assíncrono;
- arquitetura de microsserviços.
