namespace Credito.Domain;

public enum ProposalStatus { Pending, Approved, ManualReview, Rejected }

public sealed record CreditDecision(ProposalStatus Status, decimal MonthlyPayment,
    decimal IncomeCommitmentPercent, string Reason);

public sealed class Proposal
{
    public Guid Id { get; }
    public string CustomerReference { get; }
    public decimal Amount { get; }
    public int TermMonths { get; }
    public decimal MonthlyIncome { get; }
    public DateTime CreatedAtUtc { get; }
    public ProposalStatus Status { get; private set; }
    public CreditDecision? Decision { get; private set; }

    private Proposal(Guid id, string customerReference, decimal amount, int termMonths,
        decimal monthlyIncome, DateTime createdAtUtc, ProposalStatus status,
        CreditDecision? decision)
    {
        Id = id; CustomerReference = customerReference; Amount = amount;
        TermMonths = termMonths; MonthlyIncome = monthlyIncome;
        CreatedAtUtc = createdAtUtc; Status = status; Decision = decision;
    }

    public static Proposal Create(string customerReference, decimal amount, int termMonths,
        decimal monthlyIncome, DateTime createdAtUtc)
    {
        var reference = customerReference?.Trim().ToUpperInvariant() ?? "";
        if (reference.Length is < 3 or > 30 || !reference.All(char.IsLetterOrDigit))
            throw new ArgumentException("Referência do cliente deve ter 3 a 30 letras ou números.");
        if (amount < 1000m || amount > 100000m || decimal.Round(amount, 2) != amount)
            throw new ArgumentException("Valor deve estar entre 1.000 e 100.000, com até duas casas.");
        if (termMonths < 6 || termMonths > 48)
            throw new ArgumentException("Prazo deve estar entre 6 e 48 meses.");
        if (monthlyIncome < 1000m || decimal.Round(monthlyIncome, 2) != monthlyIncome)
            throw new ArgumentException("Renda mensal deve ser ao menos 1.000, com até duas casas.");
        return new Proposal(Guid.NewGuid(), reference, amount, termMonths, monthlyIncome,
            createdAtUtc.ToUniversalTime(), ProposalStatus.Pending, null);
    }

    public static Proposal Restore(Guid id, string customerReference, decimal amount, int termMonths,
        decimal monthlyIncome, DateTime createdAtUtc, ProposalStatus status, CreditDecision? decision)
        => new(id, customerReference, amount, termMonths, monthlyIncome,
            createdAtUtc, status, decision);

    public CreditDecision Analyze()
    {
        if (Decision is not null) return Decision;
        // Prestação fixa pela tabela Price, com taxa SIMULADA de 1,5% ao mês.
        const decimal rate = 0.015m;
        decimal factor = 1m;
        for (var month = 0; month < TermMonths; month++) factor *= 1m + rate;
        var payment = decimal.Round(Amount * rate * factor / (factor - 1m), 2,
            MidpointRounding.AwayFromZero);
        var percent = decimal.Round(payment / MonthlyIncome * 100m, 2,
            MidpointRounding.AwayFromZero);
        var status = percent > 30m ? ProposalStatus.Rejected
            : percent >= 25m ? ProposalStatus.ManualReview : ProposalStatus.Approved;
        var reason = status switch
        {
            ProposalStatus.Rejected => "Parcela estimada supera 30% da renda declarada.",
            ProposalStatus.ManualReview => "Comprometimento entre 25% e 30%: revisão manual necessária.",
            _ => "Comprometimento inferior a 25% da renda declarada."
        };
        return Decision = new CreditDecision(Status = status, payment, percent, reason);
    }
}
