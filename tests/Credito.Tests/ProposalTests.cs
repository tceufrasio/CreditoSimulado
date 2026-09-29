using Credito.Domain;
using Xunit;

namespace Credito.Tests;

public sealed class ProposalTests
{
    [Theory]
    [InlineData(10000, 12, 10000, ProposalStatus.Approved)]
    [InlineData(10000, 12, 3100, ProposalStatus.ManualReview)]
    [InlineData(10000, 12, 2500, ProposalStatus.Rejected)]
    public void Analyze_ClassifiesIncomeCommitment(decimal amount, int months,
        decimal income, ProposalStatus expected)
    {
        var proposal = Proposal.Create("CLIENTE1", amount, months, income, DateTime.UtcNow);
        var decision = proposal.Analyze();
        Assert.Equal(expected, decision.Status);
        Assert.Equal(expected, proposal.Status);
        Assert.Equal(decision, proposal.Analyze()); // Reanálise é idempotente.
    }

    [Fact]
    public void Create_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentException>(() => Proposal.Create("x", 10000, 12, 3000, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => Proposal.Create("ABC", 999, 12, 3000, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => Proposal.Create("ABC", 10000, 49, 3000, DateTime.UtcNow));
    }
}
