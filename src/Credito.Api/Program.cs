using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using Credito.Application;
using Credito.Domain;
using Credito.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var connection = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "Configure ConnectionStrings:Postgres.");

builder.Services.AddSingleton<IProposalRepository>(
    new PostgresProposalRepository(connection));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter()));

builder.Services.AddScoped<CreateProposalHandler>();
builder.Services.AddScoped<GetProposalHandler>();
builder.Services.AddScoped<ListProposalsHandler>();
builder.Services.AddScoped<GetDashboardHandler>();
builder.Services.AddScoped<AnalyzeProposalHandler>();
builder.Services.AddScoped<ManualDecisionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("AngularDev");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () =>
        Results.Ok(new { status = "ok" }))
    .WithTags("Health")
    .WithSummary("Verifica a disponibilidade da API")
    .Produces(StatusCodes.Status200OK);

app.MapGet("/api/dashboard", async (
        GetDashboardHandler handler,
        CancellationToken ct) =>
    {
        var dashboard = await handler.HandleAsync(ct);

        return Results.Ok(dashboard);
    })
    .WithTags("Dashboard")
    .WithSummary("Obtém o resumo das propostas de crédito")
    .Produces<ProposalDashboardSummary>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPost("/api/proposals", async (
        CreateProposalCommand input,
        CreateProposalHandler handler,
        CancellationToken ct) =>
    {
        var proposal = await handler.HandleAsync(input, ct);

        return Results.Created(
            $"/api/proposals/{proposal.Id}",
            ToResponse(proposal));
    })
    .WithTags("Proposals")
    .WithSummary("Cria uma nova proposta de crédito")
    .Produces<ProposalResponse>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/api/proposals", async (
        ProposalStatus? status,
        ListProposalsHandler handler,
        CancellationToken ct) =>
    {
        var proposals = await handler.HandleAsync(
            new ListProposalsQuery(status),
            ct);

        return Results.Ok(
            proposals.Select(ToResponse));
    })
    .WithTags("Proposals")
    .WithSummary("Lista propostas de crédito")
    .Produces<IEnumerable<ProposalResponse>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/api/proposals/{id:guid}", async (
        Guid id,
        GetProposalHandler handler,
        CancellationToken ct) =>
    {
        var proposal = await handler.HandleAsync(
            new GetProposalQuery(id),
            ct);

        return proposal is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Proposta não encontrada",
                detail: $"Não foi encontrada uma proposta com o ID {id}.")
            : Results.Ok(ToResponse(proposal));
    })
    .WithTags("Proposals")
    .WithSummary("Obtém uma proposta pelo identificador")
    .Produces<ProposalResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPost("/api/proposals/{id:guid}/analyze", async (
        Guid id,
        AnalyzeProposalHandler handler,
        CancellationToken ct) =>
    {
        var proposal = await handler.HandleAsync(
            new AnalyzeProposalCommand(id),
            ct);

        return proposal is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Proposta não encontrada",
                detail: $"Não foi encontrada uma proposta com o ID {id}.")
            : Results.Ok(ToResponse(proposal));
    })
    .WithTags("Proposals")
    .WithSummary("Executa a análise de crédito da proposta")
    .Produces<ProposalResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPost("/api/proposals/{id:guid}/manual-decision", async (
        Guid id,
        ManualDecisionRequest input,
        ManualDecisionHandler handler,
        CancellationToken ct) =>
    {
        var proposal = await handler.HandleAsync(
            new ManualDecisionCommand(
                id,
                input.Decision,
                input.Reason),
            ct);

        return proposal is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Proposta não encontrada",
                detail: $"Não foi encontrada uma proposta com o ID {id}.")
            : Results.Ok(ToResponse(proposal));
    })
    .WithTags("Proposals")
    .WithSummary("Registra a decisão manual de uma proposta em revisão")
    .Produces<ProposalResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.Run();

static ProposalResponse ToResponse(Proposal proposal) => new(
    proposal.Id,
    proposal.CustomerReference.Value,
    proposal.Amount.Value,
    proposal.TermMonths,
    proposal.MonthlyIncome.Value,
    proposal.CreatedAtUtc,
    proposal.Status.ToString(),
    proposal.Decision);

public partial class Program { }
