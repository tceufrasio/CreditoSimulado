using Credito.Domain;

namespace Credito.Application;

public interface IProposalRepository
{
    Task InsertAsync(Proposal proposal, CancellationToken cancellationToken);
    Task<Proposal?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<Proposal>> ListAsync(
        ProposalStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<ProposalDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken);
    Task<bool> SaveDecisionIfPendingAsync(Proposal proposal, CancellationToken cancellationToken);
    Task<bool> SaveManualDecisionAsync(Proposal proposal, CancellationToken cancellationToken);
}

public interface ICreditRateRepository
{
    Task<CreditRate> GetCurrentAsync(CancellationToken cancellationToken);
    Task<CreditRate> UpdateAsync(
        decimal monthlyRatePercent,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken);
}

public sealed record UpdateCreditRateCommand(decimal MonthlyRatePercent);

public sealed class GetCreditRateHandler(ICreditRateRepository repository)
{
    public Task<CreditRate> HandleAsync(CancellationToken ct)
        => repository.GetCurrentAsync(ct);
}

public sealed class UpdateCreditRateHandler(
    ICreditRateRepository repository,
    TimeProvider clock)
{
    public Task<CreditRate> HandleAsync(
        UpdateCreditRateCommand command,
        CancellationToken ct)
        => repository.UpdateAsync(
            command.MonthlyRatePercent,
            clock.GetUtcNow().UtcDateTime,
            ct);
}
public sealed record CreateProposalCommand(string CustomerReference, decimal Amount,
    int TermMonths, decimal MonthlyIncome);

public sealed class CreateProposalHandler(IProposalRepository repository, TimeProvider clock)
{
    public async Task<Proposal> HandleAsync(CreateProposalCommand command, CancellationToken ct)
    {
        var proposal = Proposal.Create(command.CustomerReference, command.Amount,
            command.TermMonths, command.MonthlyIncome, clock.GetUtcNow().UtcDateTime);
        await repository.InsertAsync(proposal, ct);
        return proposal;
    }
}

public sealed record GetProposalQuery(Guid Id);
public sealed class GetProposalHandler(IProposalRepository repository)
{
    public Task<Proposal?> HandleAsync(GetProposalQuery query, CancellationToken ct)
        => repository.GetAsync(query.Id, ct);
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems)
{
    public int TotalPages =>
        TotalItems == 0
            ? 0
            : (int)Math.Ceiling(TotalItems / (double)PageSize);
}

public sealed record ListProposalsQuery(
    ProposalStatus? Status,
    int Page = 1,
    int PageSize = 10);

public sealed class ListProposalsHandler(
    IProposalRepository repository)
{
    public Task<PagedResult<Proposal>> HandleAsync(
        ListProposalsQuery query,
        CancellationToken ct)
    {
        if (query.Page < 1)
            throw new ArgumentException(
                "A página deve ser maior ou igual a 1.");

        if (query.PageSize is < 1 or > 100)
            throw new ArgumentException(
                "O tamanho da página deve estar entre 1 e 100.");

        return repository.ListAsync(
            query.Status,
            query.Page,
            query.PageSize,
            ct);
    }
}
public sealed record ProposalDashboardSummary(
    int Total,
    int Pending,
    int Approved,
    int ManualReview,
    int Rejected,
    decimal TotalAmount);

public sealed class GetDashboardHandler(IProposalRepository repository)
{
    public Task<ProposalDashboardSummary> HandleAsync(CancellationToken ct)
        => repository.GetDashboardAsync(ct);
}
public sealed record AnalyzeProposalCommand(Guid Id);
public sealed class AnalyzeProposalHandler(
    IProposalRepository repository,
    ICreditRateRepository creditRateRepository)
{
    public async Task<Proposal?> HandleAsync(AnalyzeProposalCommand command, CancellationToken ct)
    {
        var proposal = await repository.GetAsync(command.Id, ct);
        if (proposal is null) return null;
        if (proposal.Decision is not null) return proposal;
        var creditRate = await creditRateRepository.GetCurrentAsync(ct);
        proposal.Analyze(creditRate);
        if (await repository.SaveDecisionIfPendingAsync(proposal, ct)) return proposal;
        return await repository.GetAsync(command.Id, ct); // Outra requisição decidiu primeiro.
    }
}

public sealed record ManualDecisionCommand(
    Guid Id,
    ProposalStatus Decision,
    string Reason);

public sealed class ManualDecisionHandler(
    IProposalRepository repository)
{
    public async Task<Proposal?> HandleAsync(
        ManualDecisionCommand command,
        CancellationToken ct)
    {
        var proposal = await repository.GetAsync(
            command.Id,
            ct);

        if (proposal is null)
            return null;

        switch (command.Decision)
        {
            case ProposalStatus.Approved:
                proposal.ApproveManually(command.Reason);
                break;

            case ProposalStatus.Rejected:
                proposal.RejectManually(command.Reason);
                break;

            default:
                throw new ArgumentException(
                    "A decisão manual deve ser Approved ou Rejected.");
        }

        if (await repository.SaveManualDecisionAsync(
                proposal,
                ct))
        {
            return proposal;
        }

        throw new InvalidOperationException(
            "A proposta não está mais disponível para decisão manual.");
    }
}
