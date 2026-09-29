using Npgsql;

namespace Credito.Infrastructure;

public sealed class PostgresReadinessCheck(
    string connectionString)
{
    public async Task<bool> IsReadyAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync(cancellationToken);

            await using var command =
                connection.CreateCommand();

            command.CommandText = "SELECT 1";

            var result =
                await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt32(result) == 1;
        }
        catch (Exception) when (
            !cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}