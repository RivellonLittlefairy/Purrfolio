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

    public async Task<IReadOnlyList<InvestmentRecord>> GetAllInvestmentsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.InvestmentRecords
            .AsNoTracking()
            .OrderByDescending(x => x.TradeDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteInvestmentAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.InvestmentRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.InvestmentRecords.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
