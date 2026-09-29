using Credito.Application;
using Credito.Domain;
using Npgsql;
using NpgsqlTypes;

namespace Credito.Infrastructure;

public sealed class PostgresProposalRepository(string connectionString) : IProposalRepository
{
    public async Task InsertAsync(Proposal p, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO CREDIT_PROPOSALS
            (ID, CUSTOMER_REFERENCE, AMOUNT, TERM_MONTHS, MONTHLY_INCOME, CREATED_AT_UTC, STATUS)
            VALUES (@id, @customer_reference, @amount, @term_months, @monthly_income,
                    @created_at_utc, @status)
            """;
        Add(command, "id", NpgsqlDbType.Uuid, p.Id);
        Add(command, "customer_reference", NpgsqlDbType.Varchar, p.CustomerReference.Value);
        Add(command, "amount", NpgsqlDbType.Numeric, p.Amount.Value);
        Add(command, "term_months", NpgsqlDbType.Integer, p.TermMonths);
        Add(command, "monthly_income", NpgsqlDbType.Numeric, p.MonthlyIncome.Value);
        Add(command, "created_at_utc", NpgsqlDbType.TimestampTz, p.CreatedAtUtc);
        Add(command, "status", NpgsqlDbType.Varchar, p.Status.ToString());
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<Proposal?> GetAsync(Guid id, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ID, CUSTOMER_REFERENCE, AMOUNT, TERM_MONTHS, MONTHLY_INCOME,
                   CREATED_AT_UTC, STATUS, MONTHLY_PAYMENT, COMMITMENT_PERCENT, REASON
            FROM CREDIT_PROPOSALS WHERE ID = @id
            """;
        Add(command, "id", NpgsqlDbType.Uuid, id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var status = Enum.Parse<ProposalStatus>(reader.GetString(6));
        CreditDecision? decision = reader.IsDBNull(7) ? null : new CreditDecision(status,
            reader.GetDecimal(7), reader.GetDecimal(8), reader.GetString(9));
        var createdAt = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc);
        return Proposal.Restore(reader.GetGuid(0), reader.GetString(1),
            reader.GetDecimal(2), reader.GetInt32(3), reader.GetDecimal(4), createdAt,
            status, decision);
    }

    public async Task<bool> SaveDecisionIfPendingAsync(Proposal p, CancellationToken ct)
    {
        var decision = p.Decision ?? throw new InvalidOperationException("Proposta nÃ£o analisada.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE CREDIT_PROPOSALS SET STATUS = @status, MONTHLY_PAYMENT = @payment,
                COMMITMENT_PERCENT = @percent, REASON = @reason
            WHERE ID = @id AND STATUS = 'Pending'
            """;
        Add(command, "status", NpgsqlDbType.Varchar, decision.Status.ToString());
        Add(command, "payment", NpgsqlDbType.Numeric, decision.MonthlyPayment);
        Add(command, "percent", NpgsqlDbType.Numeric, decision.IncomeCommitmentPercent);
        Add(command, "reason", NpgsqlDbType.Varchar, decision.Reason);
        Add(command, "id", NpgsqlDbType.Uuid, p.Id);
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value)
        => command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}

