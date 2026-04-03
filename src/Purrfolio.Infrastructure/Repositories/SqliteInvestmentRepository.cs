using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Purrfolio.Core.Models;
using Purrfolio.Core.Services;
using Purrfolio.Infrastructure.Data;

namespace Purrfolio.Infrastructure.Repositories;

public sealed class SqliteInvestmentRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IInvestmentRepository
{
    public async IAsyncEnumerable<InvestmentRecord> StreamInvestmentsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.InvestmentRecords
            .AsNoTracking()
            .OrderBy(x => x.TradeDate)
            .AsAsyncEnumerable();

        await foreach (var record in query.WithCancellation(cancellationToken))
        {
            yield return record;
        }
    }

    public async Task AddInvestmentAsync(InvestmentRecord record, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.InvestmentRecords.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
