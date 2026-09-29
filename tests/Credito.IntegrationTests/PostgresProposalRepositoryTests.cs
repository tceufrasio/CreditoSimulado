using Credito.Domain;
using Credito.Infrastructure;
using Npgsql;
using Xunit;

namespace Credito.IntegrationTests;

public sealed class PostgresProposalRepositoryTests
{
    [Fact]
    public async Task InsertAnalyzeAndRead_UsesPostgresAndPreventsSecondDecision()
    {
        var connectionString = Environment.GetEnvironmentVariable("CREDITO_TEST_POSTGRES")
            ?? throw new InvalidOperationException("Configure CREDITO_TEST_POSTGRES para o banco de testes.");
        var repo = new PostgresProposalRepository(connectionString);
        var proposal = Proposal.Create("TESTE" + Guid.NewGuid().ToString("N")[..8],
            10000m, 12, 4000m, DateTime.UtcNow);
        try
        {
            await repo.InsertAsync(proposal, default);
            var loaded = await repo.GetAsync(proposal.Id, default);
            Assert.NotNull(loaded);
            Assert.Equal(ProposalStatus.Pending, loaded.Status);
            loaded.Analyze();
            Assert.True(await repo.SaveDecisionIfPendingAsync(loaded, default));
            Assert.False(await repo.SaveDecisionIfPendingAsync(loaded, default));
            var saved = await repo.GetAsync(proposal.Id, default);
            Assert.Equal(ProposalStatus.Approved, saved!.Status);
            Assert.Equal(loaded.Decision, saved.Decision);
        }
        finally
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM credit_proposals WHERE id = @id";
            command.Parameters.AddWithValue("id", proposal.Id);
            await command.ExecuteNonQueryAsync();
        }
    }
}
