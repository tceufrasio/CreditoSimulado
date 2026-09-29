using Xunit;
using Credito.Domain;
using Credito.Infrastructure;
using Npgsql;

namespace Credito.IntegrationTests;

public sealed class PostgresProposalPaginationTests
{
    [Fact]
    public async Task ListAsync_PaginatesAndReturnsTotalItems()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CREDITO_TEST_POSTGRES")
            ?? throw new InvalidOperationException(
                "Configure CREDITO_TEST_POSTGRES para o banco de testes.");

        var repository =
            new PostgresProposalRepository(connectionString);

        var ids = new List<Guid>();

        try
        {
            for (var i = 0; i < 5; i++)
            {
                var proposal = Proposal.Create(
                    $"PAG{i}{Guid.NewGuid():N}".Substring(0, 20),
                    10000m + i,
                    12,
                    5000m,
                    DateTime.UtcNow.AddMilliseconds(i));

                await repository.InsertAsync(
                    proposal,
                    CancellationToken.None);

                ids.Add(proposal.Id);
            }

            var firstPage = await repository.ListAsync(
                null,
                1,
                2,
                CancellationToken.None);

            var secondPage = await repository.ListAsync(
                null,
                2,
                2,
                CancellationToken.None);

            Assert.Equal(1, firstPage.Page);
            Assert.Equal(2, firstPage.PageSize);
            Assert.Equal(2, firstPage.Items.Count);

            Assert.Equal(2, secondPage.Page);
            Assert.Equal(2, secondPage.PageSize);
            Assert.Equal(2, secondPage.Items.Count);

            Assert.True(firstPage.TotalItems >= 5);
            Assert.Equal(
                firstPage.TotalItems,
                secondPage.TotalItems);

            Assert.Equal(
                (int)Math.Ceiling(
                    firstPage.TotalItems / 2d),
                firstPage.TotalPages);

            Assert.Empty(
                firstPage.Items
                    .Select(x => x.Id)
                    .Intersect(
                        secondPage.Items.Select(x => x.Id)));
        }
        finally
        {
            if (ids.Count > 0)
            {
                await using var connection =
                    new NpgsqlConnection(connectionString);

                await connection.OpenAsync();

                await using var command =
                    connection.CreateCommand();

                command.CommandText = """
                    DELETE FROM CREDIT_PROPOSALS
                    WHERE ID = ANY(@ids)
                    """;

                command.Parameters.AddWithValue(
                    "ids",
                    ids.ToArray());

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    public async Task ListProposalsHandler_RejectsInvalidPagination(
        int page,
        int pageSize)
    {
        var repository = new FakeProposalRepository();

        var handler =
            new Credito.Application.ListProposalsHandler(
                repository);

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(
                new Credito.Application.ListProposalsQuery(
                    null,
                    page,
                    pageSize),
                CancellationToken.None));
    }

    private sealed class FakeProposalRepository
        : Credito.Application.IProposalRepository
    {
        public Task InsertAsync(
            Proposal proposal,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<Proposal?> GetAsync(
            Guid id,
            CancellationToken cancellationToken)
            => Task.FromResult<Proposal?>(null);

        public Task<Credito.Application.PagedResult<Proposal>>
            ListAsync(
                ProposalStatus? status,
                int page,
                int pageSize,
                CancellationToken cancellationToken)
            => Task.FromResult(
                new Credito.Application.PagedResult<Proposal>(
                    Array.Empty<Proposal>(),
                    page,
                    pageSize,
                    0));

        public Task<Credito.Application.ProposalDashboardSummary>
            GetDashboardAsync(
                CancellationToken cancellationToken)
            => Task.FromResult(
                new Credito.Application.ProposalDashboardSummary(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0m));

        public Task<bool> SaveDecisionIfPendingAsync(
            Proposal proposal,
            CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<bool> SaveManualDecisionAsync(
            Proposal proposal,
            CancellationToken cancellationToken)
            => Task.FromResult(true);
    }
}