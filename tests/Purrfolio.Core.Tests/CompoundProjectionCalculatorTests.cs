using Purrfolio.Core.Utilities;

namespace Purrfolio.Core.Tests;

public sealed class CompoundProjectionCalculatorTests
{
    [Fact]
    public void ProjectToTarget_ReturnsExactMonths_WhenNoReturnRate()
    {
        var result = CompoundProjectionCalculator.ProjectToTarget(
            currentBalance: 0m,
            targetBalance: 12000m,
            monthlyContribution: 1000m,
            annualizedReturnRate: 0m,
            fromDate: new DateOnly(2026, 1, 1));

        Assert.Equal(12, result.MonthsRequired);
        Assert.Equal(new DateOnly(2027, 1, 1), result.ReachTargetDate);
        Assert.Equal(12000m, result.FinalProjectedBalance);
    }

    [Fact]
    public void ProjectToTarget_ReturnsImmediately_WhenCurrentBalanceAlreadyExceedsTarget()
    {
        var result = CompoundProjectionCalculator.ProjectToTarget(
            currentBalance: 1_500_000m,
            targetBalance: 1_000_000m,
            monthlyContribution: 0m,
            annualizedReturnRate: 0m,
            fromDate: new DateOnly(2026, 1, 1));

        Assert.Equal(0, result.MonthsRequired);
        Assert.Equal(new DateOnly(2026, 1, 1), result.ReachTargetDate);
    }

    [Fact]
    public void ProjectToTarget_Throws_WhenTargetIsUnreachableWithinLimit()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CompoundProjectionCalculator.ProjectToTarget(
                currentBalance: 100m,
                targetBalance: 1000m,
                monthlyContribution: 0m,
                annualizedReturnRate: 0m,
                fromDate: new DateOnly(2026, 1, 1)));
    }
}
