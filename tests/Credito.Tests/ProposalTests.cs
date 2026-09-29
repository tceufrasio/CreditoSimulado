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

        var decision = proposal.Analyze(CreditRate.Create(1.5m, DateTime.UtcNow));

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

        var firstDecision = proposal.Analyze(CreditRate.Create(1.5m, DateTime.UtcNow));
        var secondDecision = proposal.Analyze(CreditRate.Create(1.5m, DateTime.UtcNow));

        Assert.Same(firstDecision, secondDecision);
        Assert.Equal(firstDecision, proposal.Decision);
        Assert.Equal(firstDecision.Status, proposal.Status);
    }

    [Fact]
    public void ApproveManually_WhenInManualReview_ApprovesProposal()
    {
        var proposal = Proposal.Create(
            "MANUAL001",
            50000m,
            36,
            7000m,
            DateTime.UtcNow);

        proposal.Analyze(CreditRate.Create(1.5m, DateTime.UtcNow));

        Assert.Equal(
            ProposalStatus.ManualReview,
            proposal.Status);

        var decision = proposal.ApproveManually(
            "Capacidade de pagamento validada pelo analista.");

        Assert.Equal(
            ProposalStatus.Approved,
            proposal.Status);

        Assert.Equal(
            ProposalStatus.Approved,
            decision.Status);

        Assert.Equal(
            DecisionSource.Manual,
            decision.Source);

        Assert.Equal(
            "Capacidade de pagamento validada pelo analista.",
            decision.Reason);
    }

    [Fact]
    public void RejectManually_WhenInManualReview_RejectsProposal()
    {
        var proposal = Proposal.Create(
            "MANUAL002",
            50000m,
            36,
            7000m,
            DateTime.UtcNow);

        proposal.Analyze(CreditRate.Create(1.5m, DateTime.UtcNow));

        Assert.Equal(
            ProposalStatus.ManualReview,
            proposal.Status);

        var decision = proposal.RejectManually(
            "Documentação insuficiente para aprovação.");

        Assert.Equal(
            ProposalStatus.Rejected,
            proposal.Status);

        Assert.Equal(
            ProposalStatus.Rejected,
            decision.Status);

        Assert.Equal(
            DecisionSource.Manual,
            decision.Source);
    }

    [Fact]
    public void ManualDecision_WhenProposalIsNotInManualReview_Throws()
    {
        var proposal = Proposal.Create(
            "MANUAL003",
            10000m,
            12,
            10000m,
            DateTime.UtcNow);

        proposal.Analyze(CreditRate.Create(1.5m, DateTime.UtcNow));

        Assert.Equal(
            ProposalStatus.Approved,
            proposal.Status);

        Assert.Throws<InvalidOperationException>(
            () => proposal.ApproveManually(
                "Tentativa de decisão manual."));
    }

    [Fact]
    public void ManualDecision_WithInvalidReason_Throws()
    {
        var proposal = Proposal.Create(
            "MANUAL004",
            50000m,
            36,
            7000m,
            DateTime.UtcNow);

        proposal.Analyze(CreditRate.Create(1.5m, DateTime.UtcNow));

        Assert.Throws<ArgumentException>(
            () => proposal.ApproveManually("abc"));
    }}


