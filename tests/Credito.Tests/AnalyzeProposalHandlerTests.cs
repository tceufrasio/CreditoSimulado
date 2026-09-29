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
        var rateRepo = new FakeCreditRateRepository();

        var created = Proposal.Create(
            "ABC123",
            10000,
            12,
            4000,
            DateTime.UtcNow);

        await repo.InsertAsync(created, default);

        var handler = new AnalyzeProposalHandler(repo, rateRepo);

        var first = await handler.HandleAsync(
            new AnalyzeProposalCommand(created.Id),
            default);

        var replay = await handler.HandleAsync(
            new AnalyzeProposalCommand(created.Id),
            default);

        Assert.Equal(ProposalStatus.Approved, first!.Status);
        Assert.Equal(first.Decision, replay!.Decision);
        Assert.Equal(1.5m, first.Decision!.MonthlyRatePercent);
        Assert.Equal(1, repo.SaveCount);
    }

    private sealed class FakeCreditRateRepository : ICreditRateRepository
    {
        private CreditRate rate =
            CreditRate.Create(1.5m, DateTime.UtcNow);

        public Task<CreditRate> GetCurrentAsync(
            CancellationToken cancellationToken)
            => Task.FromResult(rate);

        public Task<CreditRate> UpdateAsync(
            decimal monthlyRatePercent,
            DateTime updatedAtUtc,
            CancellationToken cancellationToken)
        {
            rate = CreditRate.Create(
                monthlyRatePercent,
                updatedAtUtc);

            return Task.FromResult(rate);
        }
    }

    private sealed class FakeRepository : IProposalRepository
    {
        private Proposal? proposal;

        public int SaveCount { get; private set; }

        public Task InsertAsync(
            Proposal p,
            CancellationToken ct)
        {
            proposal = p;
            return Task.CompletedTask;
        }

        public Task<Proposal?> GetAsync(
            Guid id,
            CancellationToken ct)
            => Task.FromResult(
                proposal?.Id == id ? proposal : null);

        public Task<IReadOnlyList<Proposal>> ListAsync(
            ProposalStatus? status,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Proposal> proposals =
                proposal is null
                    ? Array.Empty<Proposal>()
                    : new[] { proposal };

            return Task.FromResult(proposals);
        }

        public Task<PagedResult<Proposal>> ListAsync(

            ProposalStatus? status,

            int page,

            int pageSize,

            CancellationToken cancellationToken)

            => Task.FromResult(

                new PagedResult<Proposal>(

                    Array.Empty<Proposal>(),

                    page,

                    pageSize,

                    0));

        public Task<ProposalDashboardSummary> GetDashboardAsync(
            CancellationToken cancellationToken)
        {
            var summary = proposal is null
                ? new ProposalDashboardSummary(
                    0, 0, 0, 0, 0, 0m)
                : new ProposalDashboardSummary(
                    1,
                    proposal.Status == ProposalStatus.Pending ? 1 : 0,
                    proposal.Status == ProposalStatus.Approved ? 1 : 0,
                    proposal.Status == ProposalStatus.ManualReview ? 1 : 0,
                    proposal.Status == ProposalStatus.Rejected ? 1 : 0,
                    proposal.Amount.Value);

            return Task.FromResult(summary);
        }

        public Task<bool> SaveManualDecisionAsync(
            Proposal proposal,
            CancellationToken cancellationToken)
        {
            this.proposal = proposal;
            return Task.FromResult(true);
        }

        public Task<bool> SaveDecisionIfPendingAsync(
            Proposal p,
            CancellationToken ct)
        {
            SaveCount++;
            return Task.FromResult(true);
        }
    }
}
