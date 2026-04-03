using Purrfolio.Core.Models;

namespace Purrfolio.Core.Utilities;

public static class CompoundProjectionCalculator
{
    public static ProjectionResult ProjectToTarget(
        decimal currentBalance,
        decimal targetBalance,
        decimal monthlyContribution,
        decimal annualizedReturnRate,
        DateOnly? fromDate = null)
    {
        if (currentBalance < 0 || targetBalance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentBalance), "Balances must be valid positive values.");
        }

        if (monthlyContribution < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyContribution));
        }

        if (annualizedReturnRate <= -1)
        {
            throw new ArgumentOutOfRangeException(nameof(annualizedReturnRate));
        }

        if (currentBalance >= targetBalance)
        {
            var date = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
            return new ProjectionResult(0, date, currentBalance);
        }

        var monthlyRate = (decimal)Math.Pow((double)(1 + annualizedReturnRate), 1.0 / 12.0) - 1;
        var balance = currentBalance;
        var months = 0;

        while (balance < targetBalance && months < 2400)
        {
            balance = (balance + monthlyContribution) * (1 + monthlyRate);
            months++;
        }

        if (months >= 2400)
        {
            throw new InvalidOperationException("Target is unreachable within 200 years under current assumptions.");
        }

        var startDate = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var reachDate = startDate.AddMonths(months);

        return new ProjectionResult(months, reachDate, balance);
    }
}
