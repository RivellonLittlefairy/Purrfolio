using Purrfolio.Core.Models;

namespace Purrfolio.Core.Services;

public interface IInvestmentRepository
{
    IAsyncEnumerable<InvestmentRecord> StreamInvestmentsAsync(CancellationToken cancellationToken = default);

    Task AddInvestmentAsync(InvestmentRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvestmentRecord>> GetAllInvestmentsAsync(CancellationToken cancellationToken = default);

    Task<bool> DeleteInvestmentAsync(int id, CancellationToken cancellationToken = default);
}
