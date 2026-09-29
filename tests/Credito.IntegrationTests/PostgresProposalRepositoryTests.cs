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

    [Fact]
    public async Task ListAsync_ReturnsAllAndFiltersByStatus()
    {
        var connectionString = Environment.GetEnvironmentVariable("CREDITO_TEST_POSTGRES")
            ?? throw new InvalidOperationException(
                "Configure CREDITO_TEST_POSTGRES para o banco de testes.");

        var repo = new PostgresProposalRepository(connectionString);

        var referencePrefix = "LIST" + Guid.NewGuid().ToString("N")[..6];

        var pending = Proposal.Create(
            referencePrefix + "P",
            12000m,
            12,
            5000m,
            DateTime.UtcNow.AddSeconds(-1));

        var approved = Proposal.Create(
            referencePrefix + "A",
            15000m,
            12,
            10000m,
            DateTime.UtcNow);

        try
        {
            await repo.InsertAsync(pending, default);
            await repo.InsertAsync(approved, default);

            approved.Analyze();

            Assert.Equal(
                ProposalStatus.Approved,
                approved.Status);

            Assert.True(
                await repo.SaveDecisionIfPendingAsync(
                    approved,
                    default));

            var all = await repo.ListAsync(null, default);

            Assert.Contains(
                all,
                p => p.Id == pending.Id);

            Assert.Contains(
                all,
                p => p.Id == approved.Id);

            var pendingOnly = await repo.ListAsync(
                ProposalStatus.Pending,
                default);

            Assert.Contains(
                pendingOnly,
                p => p.Id == pending.Id);

            Assert.DoesNotContain(
                pendingOnly,
                p => p.Id == approved.Id);

            var approvedOnly = await repo.ListAsync(
                ProposalStatus.Approved,
                default);

            Assert.Contains(
                approvedOnly,
                p => p.Id == approved.Id);

            Assert.DoesNotContain(
                approvedOnly,
                p => p.Id == pending.Id);

            var pendingIndex = all
                .Select((proposal, index) => new { proposal.Id, index })
                .Single(x => x.Id == pending.Id)
                .index;

            var approvedIndex = all
                .Select((proposal, index) => new { proposal.Id, index })
                .Single(x => x.Id == approved.Id)
                .index;

            Assert.True(
                approvedIndex < pendingIndex,
                "A proposta mais recente deveria aparecer primeiro.");
        }
        finally
        {
            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync();

            await using var command =
                connection.CreateCommand();

            command.CommandText = """
                DELETE FROM credit_proposals
                WHERE id = @pendingId OR id = @approvedId
                """;

            command.Parameters.AddWithValue(
                "pendingId",
                pending.Id);

            command.Parameters.AddWithValue(
                "approvedId",
                approved.Id);

            await command.ExecuteNonQueryAsync();
        }
    }
    [Fact]
    public async Task GetDashboardAsync_AggregatesProposalData()
    {
        var connectionString = Environment.GetEnvironmentVariable("CREDITO_TEST_POSTGRES")
            ?? throw new InvalidOperationException(
                "Configure CREDITO_TEST_POSTGRES para o banco de testes.");

        var repo = new PostgresProposalRepository(connectionString);

        var before = await repo.GetDashboardAsync(default);

        var referencePrefix = "DASH" + Guid.NewGuid().ToString("N")[..6];

        var pending = Proposal.Create(
            referencePrefix + "P",
            10000m,
            12,
            5000m,
            DateTime.UtcNow);

        var approved = Proposal.Create(
            referencePrefix + "A",
            20000m,
            12,
            10000m,
            DateTime.UtcNow);

        try
        {
            await repo.InsertAsync(pending, default);
            await repo.InsertAsync(approved, default);

            approved.Analyze();

            Assert.Equal(
                ProposalStatus.Approved,
                approved.Status);

            Assert.True(
                await repo.SaveDecisionIfPendingAsync(
                    approved,
                    default));

            var after = await repo.GetDashboardAsync(default);

            Assert.Equal(before.Total + 2, after.Total);
            Assert.Equal(before.Pending + 1, after.Pending);
            Assert.Equal(before.Approved + 1, after.Approved);

            Assert.Equal(
                before.ManualReview,
                after.ManualReview);

            Assert.Equal(
                before.Rejected,
                after.Rejected);

            Assert.Equal(
                before.TotalAmount + 30000m,
                after.TotalAmount);
        }
        finally
        {
            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync();

            await using var command =
                connection.CreateCommand();

            command.CommandText = """
                DELETE FROM credit_proposals
                WHERE id = @pendingId OR id = @approvedId
                """;

            command.Parameters.AddWithValue(
                "pendingId",
                pending.Id);

            command.Parameters.AddWithValue(
                "approvedId",
                approved.Id);

            await command.ExecuteNonQueryAsync();
        }
    }}


