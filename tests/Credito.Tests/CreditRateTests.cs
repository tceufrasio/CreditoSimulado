using Credito.Domain;
using Xunit;

namespace Credito.Tests;

public sealed class CreditRateTests
{
    [Fact]
    public void Create_WithValidRate_CreatesActiveRate()
    {
        var createdAt = DateTime.UtcNow;

        var rate = CreditRate.Create(1.6m, createdAt);

        Assert.NotEqual(Guid.Empty, rate.Id);
        Assert.Equal(1.6m, rate.MonthlyRatePercent);
        Assert.Equal(createdAt, rate.CreatedAtUtc);
        Assert.True(rate.IsActive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void Create_WithInvalidRate_Throws(decimal value)
    {
        Assert.Throws<ArgumentException>(() =>
            CreditRate.Create(value, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithMoreThanFourDecimalPlaces_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CreditRate.Create(1.12345m, DateTime.UtcNow));
    }

    [Fact]
    public void Analyze_StoresRateUsedInDecision()
    {
        var proposal = Proposal.Create(
            "RATE001",
            50000m,
            36,
            7000m,
            DateTime.UtcNow);

        var rate = CreditRate.Create(
            1.6m,
            DateTime.UtcNow);

        var decision = proposal.Analyze(rate);

        Assert.Equal(1.6m, decision.MonthlyRatePercent);
        Assert.Equal(1837.86m, decision.MonthlyPayment);
        Assert.Equal(26.26m, decision.IncomeCommitmentPercent);
        Assert.Equal(ProposalStatus.ManualReview, decision.Status);
    }
}
