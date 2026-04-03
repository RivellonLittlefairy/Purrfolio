using Purrfolio.App.Models;

namespace Purrfolio.App.Services;

public interface IMinimaxOcrService
{
    Task<IReadOnlyList<OcrInvestmentCandidate>> AnalyzeScreenshotAsync(
        string imagePath,
        MinimaxRequestOptions options,
        CancellationToken cancellationToken = default);
}
