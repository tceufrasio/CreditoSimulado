namespace Credito.Domain;

public sealed record Money
{
    public decimal Value { get; }

    private Money(decimal value)
    {
        Value = value;
    }

    public static Money Create(decimal value)
    {
        if (value < 0m)
            throw new ArgumentException("Valor monetário não pode ser negativo.");

        if (decimal.Round(value, 2) != value)
            throw new ArgumentException("Valor monetário deve possuir no máximo duas casas decimais.");

        return new Money(value);
    }

    public override string ToString() => Value.ToString("0.00");

    public static implicit operator decimal(Money money)
        => money.Value;
}
