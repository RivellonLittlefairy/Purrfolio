using Purrfolio.Core.Models;
using Purrfolio.Core.Utilities;

namespace Purrfolio.Core.Tests;

public sealed class XirrCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsExpectedRate_ForSingleYearCashFlow()
    {
        var cashFlows = new[]
        {
            new CashFlow(new DateTime(2024, 1, 1), -1000m),
            new CashFlow(new DateTime(2025, 1, 1), 1100m)
        };

        var result = XirrCalculator.Calculate(cashFlows);

        Assert.InRange(result, 0.099m, 0.101m);
    }

    [Fact]
    public void Calculate_ReturnsExpectedRate_ForTwoYearCashFlow()
    {
        var cashFlows = new[]
        {
            new CashFlow(new DateTime(2024, 1, 1), -1000m),
            new CashFlow(new DateTime(2026, 1, 1), 1210m)
        };

        var result = XirrCalculator.Calculate(cashFlows);

        Assert.InRange(result, 0.099m, 0.101m);
    }

    [Fact]
    public void Calculate_Throws_WhenCashFlowsHaveSameSign()
    {
        var cashFlows = new[]
        {
            new CashFlow(new DateTime(2024, 1, 1), -1000m),
            new CashFlow(new DateTime(2024, 3, 1), -500m)
        };

        Assert.Throws<ArgumentException>(() => XirrCalculator.Calculate(cashFlows));
    }
}
