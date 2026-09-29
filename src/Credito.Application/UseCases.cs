using Credito.Domain;

namespace Credito.Application;

public interface IProposalRepository
{
    Task InsertAsync(Proposal proposal, CancellationToken cancellationToken);
    Task<Proposal?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SaveDecisionIfPendingAsync(Proposal proposal, CancellationToken cancellationToken);
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

public sealed record AnalyzeProposalCommand(Guid Id);
public sealed class AnalyzeProposalHandler(IProposalRepository repository)
{
    public async Task<Proposal?> HandleAsync(AnalyzeProposalCommand command, CancellationToken ct)
    {
        var proposal = await repository.GetAsync(command.Id, ct);
        if (proposal is null) return null;
        if (proposal.Decision is not null) return proposal;
        proposal.Analyze();
        if (await repository.SaveDecisionIfPendingAsync(proposal, ct)) return proposal;
        return await repository.GetAsync(command.Id, ct); // Outra requisição decidiu primeiro.
    }
}
