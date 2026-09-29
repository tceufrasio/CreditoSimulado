namespace Credito.Domain;

public sealed record CustomerReference
{
    public string Value { get; }

    private CustomerReference(string value)
    {
        Value = value;
    }

    public static CustomerReference Create(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (normalized.Length is < 3 or > 30 ||
            !normalized.All(char.IsLetterOrDigit))
        {
            throw new ArgumentException(
                "Referência do cliente deve ter 3 a 30 letras ou números.");
        }

        return new CustomerReference(normalized);
    }

    public override string ToString() => Value;

    public static implicit operator string(CustomerReference reference)
        => reference.Value;
}
