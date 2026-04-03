using Purrfolio.Core.Models;

namespace Purrfolio.Core.Services;

public interface IInvestmentRepository
{
    IAsyncEnumerable<InvestmentRecord> StreamInvestmentsAsync(CancellationToken cancellationToken = default);

    Task AddInvestmentAsync(InvestmentRecord record, CancellationToken cancellationToken = default);
}
