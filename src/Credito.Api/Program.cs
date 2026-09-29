using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using Credito.Application;
using Credito.Domain;
using Credito.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
var connection = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Postgres.");
builder.Services.AddSingleton<IProposalRepository>(new PostgresProposalRepository(connection));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<CreateProposalHandler>();
builder.Services.AddScoped<GetProposalHandler>();
builder.Services.AddScoped<AnalyzeProposalHandler>();
var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/proposals", async (CreateProposalCommand input,
    CreateProposalHandler handler, CancellationToken ct) =>
{
    var p = await handler.HandleAsync(input, ct);
    return Results.Created($"/api/proposals/{p.Id}", ToResponse(p));
});

app.MapGet("/api/proposals/{id:guid}", async (Guid id,
    GetProposalHandler handler, CancellationToken ct) =>
{
    var p = await handler.HandleAsync(new GetProposalQuery(id), ct);
    return p is null ? Results.Problem(statusCode: 404, title: "Proposta não encontrada", detail: $"Não foi encontrada uma proposta com o ID {id}.") : Results.Ok(ToResponse(p));
});

app.MapPost("/api/proposals/{id:guid}/analyze", async (Guid id,
    AnalyzeProposalHandler handler, CancellationToken ct) =>
{
    var p = await handler.HandleAsync(new AnalyzeProposalCommand(id), ct);
    return p is null ? Results.Problem(statusCode: 404, title: "Proposta não encontrada", detail: $"Não foi encontrada uma proposta com o ID {id}.") : Results.Ok(ToResponse(p));
});

app.Run();

static ProposalResponse ToResponse(Proposal p) => new(
    p.Id,
    p.CustomerReference,
    p.Amount,
    p.TermMonths,
    p.MonthlyIncome,
    p.CreatedAtUtc,
    p.Status.ToString(),
    p.Decision);

public partial class Program { }






