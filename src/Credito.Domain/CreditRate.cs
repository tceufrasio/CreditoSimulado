namespace Credito.Domain;

public sealed class CreditRate
{
    public Guid Id { get; private set; }
    public decimal MonthlyRatePercent { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public bool IsActive { get; private set; }

    private CreditRate()
    {
    }

    private CreditRate(
        Guid id,
        decimal monthlyRatePercent,
        DateTime createdAtUtc,
        bool isActive)
    {
        Id = id;
        MonthlyRatePercent = Validate(monthlyRatePercent);
        CreatedAtUtc = createdAtUtc;
        IsActive = isActive;
    }

    public static CreditRate Create(
        decimal monthlyRatePercent,
        DateTime createdAtUtc)
    {
        return new CreditRate(
            Guid.NewGuid(),
            monthlyRatePercent,
            createdAtUtc,
            true);
    }

    public static CreditRate Restore(
        Guid id,
        decimal monthlyRatePercent,
        DateTime createdAtUtc,
        bool isActive)
    {
        return new CreditRate(
            id,
            monthlyRatePercent,
            createdAtUtc,
            isActive);
    }

    private static decimal Validate(decimal rate)
    {
        if (rate <= 0)
            throw new ArgumentException(
                "A taxa mensal deve ser maior que zero.");

        if (rate > 100)
            throw new ArgumentException(
                "A taxa mensal não pode ser maior que 100%.");

        if (decimal.Round(rate, 4) != rate)
            throw new ArgumentException(
                "A taxa mensal deve possuir no máximo 4 casas decimais.");

        return rate;
    }
}
