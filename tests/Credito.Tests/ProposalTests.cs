using Credito.Domain;
using Xunit;

namespace Credito.Tests;

public sealed class ProposalTests
{
    [Theory]
    [InlineData(10000, 12, 10000, ProposalStatus.Approved)]
    [InlineData(10000, 12, 3100, ProposalStatus.ManualReview)]
    [InlineData(10000, 12, 2500, ProposalStatus.Rejected)]
    public void Analyze_ClassifiesIncomeCommitment(
        decimal amount,
        int months,
        decimal income,
        ProposalStatus expected)
    {
        var proposal = Proposal.Create(
            "CLIENTE1",
            amount,
            months,
            income,
            DateTime.UtcNow);

        var decision = proposal.Analyze();

        Assert.Equal(expected, decision.Status);
        Assert.Equal(expected, proposal.Status);
    }

    [Fact]
    public void Create_StartsProposalAsPending()
    {
        var proposal = Proposal.Create(
            "cliente001",
            10000m,
            12,
            5000m,
            DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, proposal.Id);
        Assert.Equal("CLIENTE001", proposal.CustomerReference.Value);
        Assert.Equal(10000m, proposal.Amount.Value);
        Assert.Equal(5000m, proposal.MonthlyIncome.Value);
        Assert.Equal(ProposalStatus.Pending, proposal.Status);
        Assert.Null(proposal.Decision);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(100001)]
    public void Create_RejectsAmountOutsideAllowedRange(decimal amount)
    {
        Assert.Throws<ArgumentException>(() =>
            Proposal.Create(
                "CLIENTE1",
                amount,
                12,
                5000m,
                DateTime.UtcNow));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(49)]
    public void Create_RejectsTermOutsideAllowedRange(int months)
    {
        Assert.Throws<ArgumentException>(() =>
            Proposal.Create(
                "CLIENTE1",
                10000m,
                months,
                5000m,
                DateTime.UtcNow));
    }

    [Fact]
    public void Create_RejectsIncomeBelowMinimum()
    {
        Assert.Throws<ArgumentException>(() =>
            Proposal.Create(
                "CLIENTE1",
                10000m,
                12,
                999m,
                DateTime.UtcNow));
    }

    [Fact]
    public void Analyze_IsIdempotent()
    {
        var proposal = Proposal.Create(
            "CLIENTE1",
            10000m,
            12,
            5000m,
            DateTime.UtcNow);

        var firstDecision = proposal.Analyze();
        var secondDecision = proposal.Analyze();

        Assert.Same(firstDecision, secondDecision);
        Assert.Equal(firstDecision, proposal.Decision);
        Assert.Equal(firstDecision.Status, proposal.Status);
    }
}
