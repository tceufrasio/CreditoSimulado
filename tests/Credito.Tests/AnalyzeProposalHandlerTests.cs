using Credito.Application;
using Credito.Domain;
using Xunit;

namespace Credito.Tests;

public sealed class AnalyzeProposalHandlerTests
{
    [Fact]
    public async Task Analyze_SavesOnceAndReplaysDecision()
    {
        var repo = new FakeRepository();
        var created = Proposal.Create("ABC123", 10000, 12, 4000, DateTime.UtcNow);
        await repo.InsertAsync(created, default);
        var handler = new AnalyzeProposalHandler(repo);
        var first = await handler.HandleAsync(new AnalyzeProposalCommand(created.Id), default);
        var replay = await handler.HandleAsync(new AnalyzeProposalCommand(created.Id), default);
        Assert.Equal(ProposalStatus.Approved, first!.Status);
        Assert.Equal(first.Decision, replay!.Decision);
        Assert.Equal(1, repo.SaveCount);
    }

    private sealed class FakeRepository : IProposalRepository
    {
        private Proposal? proposal;
        public int SaveCount { get; private set; }
        public Task InsertAsync(Proposal p, CancellationToken ct) { proposal = p; return Task.CompletedTask; }
        public Task<Proposal?> GetAsync(Guid id, CancellationToken ct)
            => Task.FromResult(proposal?.Id == id ? proposal : null);
        public Task<bool> SaveDecisionIfPendingAsync(Proposal p, CancellationToken ct)
        { SaveCount++; return Task.FromResult(true); }
    }
}
