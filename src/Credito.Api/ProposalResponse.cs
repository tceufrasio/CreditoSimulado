using Credito.Domain;

public sealed record ProposalResponse(
    Guid Id,
    string CustomerReference,
    decimal Amount,
    int TermMonths,
    decimal MonthlyIncome,
    DateTime CreatedAtUtc,
    string Status,
    CreditDecision? Decision);

