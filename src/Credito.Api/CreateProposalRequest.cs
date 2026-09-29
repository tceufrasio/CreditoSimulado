public sealed record CreateProposalRequest(
    string CustomerReference,
    decimal Amount,
    int TermMonths,
    decimal MonthlyIncome);