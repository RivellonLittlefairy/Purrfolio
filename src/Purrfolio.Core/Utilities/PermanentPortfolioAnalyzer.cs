using Purrfolio.Core.Enums;
using Purrfolio.Core.Models;

namespace Purrfolio.Core.Utilities;

public static class PermanentPortfolioAnalyzer
{
    private static readonly IReadOnlyDictionary<AssetClass, decimal> DefaultTargetWeights =
        new Dictionary<AssetClass, decimal>
        {
            [AssetClass.Stocks] = 0.25m,
            [AssetClass.Gold] = 0.25m,
            [AssetClass.GovernmentBonds] = 0.25m,
            [AssetClass.Cash] = 0.25m
        };

    public static IReadOnlyList<PortfolioDeviationItem> Analyze(
        IReadOnlyDictionary<AssetClass, decimal> currentAllocation,
        decimal threshold = 0.05m,
        IReadOnlyDictionary<AssetClass, decimal>? targetWeights = null)
    {
        var targets = targetWeights ?? DefaultTargetWeights;
        var total = currentAllocation.Values.Sum();
        if (total <= 0)
        {
            return targets
                .Select(t => new PortfolioDeviationItem(t.Key, 0, t.Value, -t.Value, t.Value > threshold))
                .ToArray();
        }

        return targets
            .Select(t =>
            {
                currentAllocation.TryGetValue(t.Key, out var value);
                var weight = value / total;
                var deviation = weight - t.Value;
                return new PortfolioDeviationItem(
                    t.Key,
                    weight,
                    t.Value,
                    deviation,
                    Math.Abs(deviation) > threshold);
            })
            .OrderBy(x => x.AssetClass)
            .ToArray();
    }
}
