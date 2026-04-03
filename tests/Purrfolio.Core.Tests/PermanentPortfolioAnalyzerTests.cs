using Purrfolio.Core.Enums;
using Purrfolio.Core.Utilities;

namespace Purrfolio.Core.Tests;

public sealed class PermanentPortfolioAnalyzerTests
{
    [Fact]
    public void Analyze_FlagsDeviation_WhenThresholdExceeded()
    {
        var allocation = new Dictionary<AssetClass, decimal>
        {
            [AssetClass.Stocks] = 600m,
            [AssetClass.Gold] = 100m,
            [AssetClass.GovernmentBonds] = 200m,
            [AssetClass.Cash] = 100m
        };

        var result = PermanentPortfolioAnalyzer.Analyze(allocation, threshold: 0.05m);

        var stocks = result.Single(x => x.AssetClass == AssetClass.Stocks);
        Assert.True(stocks.IsAlert);

        var bonds = result.Single(x => x.AssetClass == AssetClass.GovernmentBonds);
        Assert.False(bonds.IsAlert);
    }
}
