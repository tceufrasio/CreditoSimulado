namespace Credito.Domain;

public enum ProposalStatus
{
    Pending,
    Approved,
    ManualReview,
    Rejected
}

public enum DecisionSource
{
    Automatic,
    Manual
}

public sealed record CreditDecision(
    ProposalStatus Status,
    decimal MonthlyPayment,
    decimal IncomeCommitmentPercent,
    string Reason,
    DecisionSource Source = DecisionSource.Automatic);

public sealed class Proposal
{
    public Guid Id { get; }
    public CustomerReference CustomerReference { get; }
    public Money Amount { get; }
    public int TermMonths { get; }
    public Money MonthlyIncome { get; }
    public DateTime CreatedAtUtc { get; }
    public ProposalStatus Status { get; private set; }
    public CreditDecision? Decision { get; private set; }

    private Proposal(
        Guid id,
        CustomerReference customerReference,
        Money amount,
        int termMonths,
        Money monthlyIncome,
        DateTime createdAtUtc,
        ProposalStatus status,
        CreditDecision? decision)
    {
        Id = id;
        CustomerReference = customerReference;
        Amount = amount;
        TermMonths = termMonths;
        MonthlyIncome = monthlyIncome;
        CreatedAtUtc = createdAtUtc;
        Status = status;
        Decision = decision;
    }

    public static Proposal Create(
        string customerReference,
        decimal amount,
        int termMonths,
        decimal monthlyIncome,
        DateTime createdAtUtc)
    {
        var reference = CustomerReference.Create(customerReference);
        var proposalAmount = Money.Create(amount);
        var income = Money.Create(monthlyIncome);

        if (proposalAmount.Value < 1000m || proposalAmount.Value > 100000m)
            throw new ArgumentException(
                "Valor deve estar entre 1.000 e 100.000.");

        if (termMonths is < 6 or > 48)
            throw new ArgumentException(
                "Prazo deve estar entre 6 e 48 meses.");

        if (income.Value < 1000m)
            throw new ArgumentException(
                "Renda mensal deve ser ao menos 1.000.");

        return new Proposal(
            Guid.NewGuid(),
            reference,
            proposalAmount,
            termMonths,
            income,
            createdAtUtc.ToUniversalTime(),
            ProposalStatus.Pending,
            null);
    }

    public static Proposal Restore(
        Guid id,
        string customerReference,
        decimal amount,
        int termMonths,
        decimal monthlyIncome,
        DateTime createdAtUtc,
        ProposalStatus status,
        CreditDecision? decision)
        => new(
            id,
            CustomerReference.Create(customerReference),
            Money.Create(amount),
            termMonths,
            Money.Create(monthlyIncome),
            createdAtUtc,
            status,
            decision);

    public CreditDecision ApproveManually(string reason)
    {
        EnsureManualReview();

        var normalizedReason = ValidateManualReason(reason);

        Decision = Decision! with
        {
            Status = ProposalStatus.Approved,
            Reason = normalizedReason,
            Source = DecisionSource.Manual
        };

        Status = ProposalStatus.Approved;

        return Decision;
    }

    public CreditDecision RejectManually(string reason)
    {
        EnsureManualReview();

        var normalizedReason = ValidateManualReason(reason);

        Decision = Decision! with
        {
            Status = ProposalStatus.Rejected,
            Reason = normalizedReason,
            Source = DecisionSource.Manual
        };

        Status = ProposalStatus.Rejected;

        return Decision;
    }

    private void EnsureManualReview()
    {
        if (Status != ProposalStatus.ManualReview ||
            Decision is null)
        {
            throw new InvalidOperationException(
                "Somente propostas em revisão manual podem receber uma decisão manual.");
        }
    }

    private static string ValidateManualReason(string? reason)
    {
        var normalizedReason = reason?.Trim() ?? string.Empty;

        if (normalizedReason.Length < 5)
        {
            throw new ArgumentException(
                "Informe um motivo com pelo menos 5 caracteres.");
        }

        if (normalizedReason.Length > 250)
        {
            throw new ArgumentException(
                "O motivo deve possuir no máximo 250 caracteres.");
        }

        return normalizedReason;
    }
    public CreditDecision Analyze()
    {
        if (Decision is not null)
            return Decision;

        // Prestação fixa pela Tabela Price com taxa simulada de 1,5% ao mês.
        const decimal rate = 0.015m;

        decimal factor = 1m;

        for (var month = 0; month < TermMonths; month++)
            factor *= 1m + rate;

        var payment = decimal.Round(
            Amount.Value * rate * factor / (factor - 1m),
            2,
            MidpointRounding.AwayFromZero);

        var percent = decimal.Round(
            payment / MonthlyIncome.Value * 100m,
            2,
            MidpointRounding.AwayFromZero);

        var status = percent switch
        {
            > 30m => ProposalStatus.Rejected,
            >= 25m => ProposalStatus.ManualReview,
            _ => ProposalStatus.Approved
        };

        var reason = status switch
        {
            ProposalStatus.Rejected =>
                "Parcela estimada supera 30% da renda declarada.",

            ProposalStatus.ManualReview =>
                "Comprometimento entre 25% e 30%: revisão manual necessária.",

            _ =>
                "Comprometimento inferior a 25% da renda declarada."
        };

        Decision = new CreditDecision(
            status,
            payment,
            percent,
            reason);

        Status = status;

        return Decision;
    }
}
