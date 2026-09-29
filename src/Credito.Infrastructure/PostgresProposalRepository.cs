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
                   CREATED_AT_UTC, STATUS, MONTHLY_PAYMENT, COMMITMENT_PERCENT, REASON, INTEREST_RATE_PERCENT, DECISION_SOURCE
            FROM CREDIT_PROPOSALS WHERE ID = @id
            """;
        Add(command, "id", NpgsqlDbType.Uuid, id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var status = Enum.Parse<ProposalStatus>(reader.GetString(6));
        CreditDecision? decision = reader.IsDBNull(7)
            ? null
            : new CreditDecision(
                status,
                reader.GetDecimal(7),
                reader.GetDecimal(8),
                reader.GetString(9),
                reader.IsDBNull(10) ? 1.5m : reader.GetDecimal(10),
                reader.IsDBNull(11)
                    ? DecisionSource.Automatic
                    : Enum.Parse<DecisionSource>(reader.GetString(11)));
        var createdAt = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc);
        return Proposal.Restore(reader.GetGuid(0), reader.GetString(1),
            reader.GetDecimal(2), reader.GetInt32(3), reader.GetDecimal(4), createdAt,
            status, decision);
    }

    public async Task<PagedResult<Proposal>> ListAsync(
        ProposalStatus? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync(ct);

        var statusValue =
            status?.ToString() ?? (object)DBNull.Value;

        int totalItems;

        await using (var countCommand =
                     connection.CreateCommand())
        {
            countCommand.CommandText = """
                SELECT COUNT(*)::int
                FROM CREDIT_PROPOSALS
                WHERE (@status IS NULL OR STATUS = @status)
                """;

            countCommand.Parameters.Add(
                new NpgsqlParameter(
                    "status",
                    NpgsqlDbType.Varchar)
                {
                    Value = statusValue
                });

            totalItems = Convert.ToInt32(
                await countCommand.ExecuteScalarAsync(ct));
        }

        var offset = (page - 1) * pageSize;

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT ID,
                   CUSTOMER_REFERENCE,
                   AMOUNT,
                   TERM_MONTHS,
                   MONTHLY_INCOME,
                   CREATED_AT_UTC,
                   STATUS,
                   MONTHLY_PAYMENT,
                   COMMITMENT_PERCENT,
                   REASON,
                   INTEREST_RATE_PERCENT,
                   DECISION_SOURCE
            FROM CREDIT_PROPOSALS
            WHERE (@status IS NULL OR STATUS = @status)
            ORDER BY CREATED_AT_UTC DESC, ID DESC
            LIMIT @page_size
            OFFSET @offset
            """;

        command.Parameters.Add(
            new NpgsqlParameter(
                "status",
                NpgsqlDbType.Varchar)
            {
                Value = statusValue
            });

        command.Parameters.Add(
            new NpgsqlParameter(
                "page_size",
                NpgsqlDbType.Integer)
            {
                Value = pageSize
            });

        command.Parameters.Add(
            new NpgsqlParameter(
                "offset",
                NpgsqlDbType.Integer)
            {
                Value = offset
            });

        var proposals = new List<Proposal>();

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var proposalStatus =
                Enum.Parse<ProposalStatus>(
                    reader.GetString(6));

            CreditDecision? decision =
                reader.IsDBNull(7)
                    ? null
                    : new CreditDecision(
                        proposalStatus,
                        reader.GetDecimal(7),
                        reader.GetDecimal(8),
                        reader.GetString(9),
                        reader.IsDBNull(10)
                            ? 1.5m
                            : reader.GetDecimal(10),
                        reader.IsDBNull(11)
                            ? DecisionSource.Automatic
                            : Enum.Parse<DecisionSource>(
                                reader.GetString(11)));

            var createdAt =
                DateTime.SpecifyKind(
                    reader.GetDateTime(5),
                    DateTimeKind.Utc);

            proposals.Add(
                Proposal.Restore(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetDecimal(2),
                    reader.GetInt32(3),
                    reader.GetDecimal(4),
                    createdAt,
                    proposalStatus,
                    decision));
        }

        return new PagedResult<Proposal>(
            proposals,
            page,
            pageSize,
            totalItems);
    }
    public async Task<ProposalDashboardSummary> GetDashboardAsync(
        CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                COUNT(*)::int AS total,
                COUNT(*) FILTER (WHERE STATUS = 'Pending')::int AS pending,
                COUNT(*) FILTER (WHERE STATUS = 'Approved')::int AS approved,
                COUNT(*) FILTER (WHERE STATUS = 'ManualReview')::int AS manual_review,
                COUNT(*) FILTER (WHERE STATUS = 'Rejected')::int AS rejected,
                COALESCE(SUM(AMOUNT), 0) AS total_amount
            FROM CREDIT_PROPOSALS
            """;

        await using var reader = await command.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
            return new ProposalDashboardSummary(0, 0, 0, 0, 0, 0m);

        return new ProposalDashboardSummary(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetDecimal(5));
    }
    public async Task<bool> SaveDecisionIfPendingAsync(Proposal p, CancellationToken ct)
    {
        var decision = p.Decision ?? throw new InvalidOperationException("Proposta não analisada.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE CREDIT_PROPOSALS
            SET STATUS = @status,
                MONTHLY_PAYMENT = @payment,
                COMMITMENT_PERCENT = @percent,
                REASON = @reason,
                INTEREST_RATE_PERCENT = @interest_rate_percent,
                DECISION_SOURCE = @decision_source
            WHERE ID = @id
              AND STATUS = 'Pending'
            """;
        Add(command, "status", NpgsqlDbType.Varchar, decision.Status.ToString());
        Add(command, "payment", NpgsqlDbType.Numeric, decision.MonthlyPayment);
        Add(command, "percent", NpgsqlDbType.Numeric, decision.IncomeCommitmentPercent);
        Add(command, "reason", NpgsqlDbType.Varchar, decision.Reason);
        Add(command, "interest_rate_percent", NpgsqlDbType.Numeric, decision.MonthlyRatePercent);
        Add(command, "decision_source", NpgsqlDbType.Varchar, decision.Source.ToString());
        Add(command, "id", NpgsqlDbType.Uuid, p.Id);
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }

    public async Task<bool> SaveManualDecisionAsync(
        Proposal proposal,
        CancellationToken ct)
    {
        var decision = proposal.Decision
            ?? throw new InvalidOperationException(
                "Proposta não possui decisão.");

        if (decision.Source != DecisionSource.Manual)
        {
            throw new InvalidOperationException(
                "A decisão informada não é manual.");
        }

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync(ct);

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            UPDATE CREDIT_PROPOSALS
            SET STATUS = @status,
                REASON = @reason,
                DECISION_SOURCE = @decision_source
            WHERE ID = @id
              AND STATUS = 'ManualReview'
            """;

        Add(
            command,
            "status",
            NpgsqlDbType.Varchar,
            decision.Status.ToString());

        Add(
            command,
            "reason",
            NpgsqlDbType.Varchar,
            decision.Reason);

        Add(
            command,
            "decision_source",
            NpgsqlDbType.Varchar,
            decision.Source.ToString());

        Add(
            command,
            "id",
            NpgsqlDbType.Uuid,
            proposal.Id);

        return await command.ExecuteNonQueryAsync(ct) == 1;
    }
    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value)
        => command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
