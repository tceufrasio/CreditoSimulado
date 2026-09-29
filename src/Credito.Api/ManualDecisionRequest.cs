using Credito.Domain;

public sealed record ManualDecisionRequest(
    ProposalStatus Decision,
    string Reason);
