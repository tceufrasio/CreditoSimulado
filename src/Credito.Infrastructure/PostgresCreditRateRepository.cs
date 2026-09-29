using Credito.Application;
using Credito.Domain;
using Npgsql;
using NpgsqlTypes;

namespace Credito.Infrastructure;

public sealed class PostgresCreditRateRepository(
    string connectionString) : ICreditRateRepository
{
    public async Task<CreditRate> GetCurrentAsync(
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT ID,
                   MONTHLY_RATE_PERCENT,
                   CREATED_AT_UTC,
                   IS_ACTIVE
            FROM CREDIT_RATES
            WHERE IS_ACTIVE = true
            LIMIT 1
            """;

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException(
                "Nenhuma taxa de crédito ativa foi encontrada.");
        }

        var createdAt = DateTime.SpecifyKind(
            reader.GetDateTime(2),
            DateTimeKind.Utc);

        return CreditRate.Restore(
            reader.GetGuid(0),
            reader.GetDecimal(1),
            createdAt,
            reader.GetBoolean(3));
    }

    public async Task<CreditRate> UpdateAsync(
        decimal monthlyRatePercent,
        DateTime updatedAtUtc,
        CancellationToken ct)
    {
        var rate = CreditRate.Create(
            monthlyRatePercent,
            updatedAtUtc);

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync(ct);

        await using var transaction =
            await connection.BeginTransactionAsync(ct);

        await using (var deactivate =
                     connection.CreateCommand())
        {
            deactivate.Transaction = transaction;
            deactivate.CommandText = """
                UPDATE CREDIT_RATES
                SET IS_ACTIVE = false
                WHERE IS_ACTIVE = true
                """;

            await deactivate.ExecuteNonQueryAsync(ct);
        }

        await using (var insert =
                     connection.CreateCommand())
        {
            insert.Transaction = transaction;

            insert.CommandText = """
                INSERT INTO CREDIT_RATES
                    (ID,
                     MONTHLY_RATE_PERCENT,
                     CREATED_AT_UTC,
                     IS_ACTIVE)
                VALUES
                    (@id,
                     @monthly_rate_percent,
                     @created_at_utc,
                     true)
                """;

            insert.Parameters.Add(
                new NpgsqlParameter(
                    "id",
                    NpgsqlDbType.Uuid)
                {
                    Value = rate.Id
                });

            insert.Parameters.Add(
                new NpgsqlParameter(
                    "monthly_rate_percent",
                    NpgsqlDbType.Numeric)
                {
                    Value = rate.MonthlyRatePercent
                });

            insert.Parameters.Add(
                new NpgsqlParameter(
                    "created_at_utc",
                    NpgsqlDbType.TimestampTz)
                {
                    Value = rate.CreatedAtUtc
                });

            await insert.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);

        return rate;
    }
}
