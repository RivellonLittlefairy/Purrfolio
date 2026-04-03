using Purrfolio.Core.Models;

namespace Purrfolio.Core.Utilities;

public static class XirrCalculator
{
    private const double MinRate = -0.99999999;
    private const double MaxRate = 1000.0;
    private const int MaxIterations = 100;
    private const double Tolerance = 1e-10;

    public static decimal Calculate(IReadOnlyCollection<CashFlow> cashFlows, decimal initialGuess = 0.10m)
    {
        if (cashFlows.Count < 2)
        {
            throw new ArgumentException("XIRR requires at least two cash flows.", nameof(cashFlows));
        }

        var hasPositive = cashFlows.Any(c => c.Amount > 0);
        var hasNegative = cashFlows.Any(c => c.Amount < 0);
        if (!hasPositive || !hasNegative)
        {
            throw new ArgumentException("Cash flows must contain at least one positive and one negative value.", nameof(cashFlows));
        }

        var ordered = cashFlows.OrderBy(c => c.Date).ToArray();
        var startDate = ordered[0].Date;

        var guess = Math.Clamp((double)initialGuess, MinRate + 1e-6, MaxRate);

        for (var i = 0; i < MaxIterations; i++)
        {
            var (f, df) = NpvAndDerivative(ordered, startDate, guess);

            if (Math.Abs(f) < Tolerance)
            {
                return (decimal)guess;
            }

            if (Math.Abs(df) < Tolerance)
            {
                break;
            }

            var next = guess - (f / df);
            if (double.IsNaN(next) || double.IsInfinity(next) || next <= MinRate)
            {
                break;
            }

            if (Math.Abs(next - guess) < Tolerance)
            {
                return (decimal)next;
            }

            guess = next;
        }

        var bisectResult = Bisect(ordered, startDate);
        return (decimal)bisectResult;
    }

    private static (double f, double df) NpvAndDerivative(IReadOnlyList<CashFlow> cashFlows, DateTime startDate, double rate)
    {
        double npv = 0;
        double derivative = 0;

        foreach (var cf in cashFlows)
        {
            var years = (cf.Date - startDate).TotalDays / 365.0;
            var baseVal = Math.Pow(1.0 + rate, years);
            npv += (double)cf.Amount / baseVal;
            derivative += -years * (double)cf.Amount / (baseVal * (1.0 + rate));
        }

        return (npv, derivative);
    }

    private static double Bisect(IReadOnlyList<CashFlow> cashFlows, DateTime startDate)
    {
        var lower = MinRate + 1e-8;
        var upper = 10.0;

        var npvLower = Npv(cashFlows, startDate, lower);
        var npvUpper = Npv(cashFlows, startDate, upper);

        var expansions = 0;
        while (npvLower * npvUpper > 0 && upper < MaxRate && expansions < 20)
        {
            upper *= 2;
            npvUpper = Npv(cashFlows, startDate, upper);
            expansions++;
        }

        if (npvLower * npvUpper > 0)
        {
            throw new InvalidOperationException("Unable to bracket XIRR root for the provided cash flow series.");
        }

        for (var i = 0; i < 200; i++)
        {
            var mid = (lower + upper) / 2.0;
            var npvMid = Npv(cashFlows, startDate, mid);

            if (Math.Abs(npvMid) < Tolerance)
            {
                return mid;
            }

            if (npvLower * npvMid < 0)
            {
                upper = mid;
                npvUpper = npvMid;
            }
            else
            {
                lower = mid;
                npvLower = npvMid;
            }

            if (Math.Abs(upper - lower) < Tolerance)
            {
                return (upper + lower) / 2.0;
            }
        }

        return (upper + lower) / 2.0;
    }

    private static double Npv(IReadOnlyList<CashFlow> cashFlows, DateTime startDate, double rate)
    {
        double npv = 0;

        foreach (var cf in cashFlows)
        {
            var years = (cf.Date - startDate).TotalDays / 365.0;
            npv += (double)cf.Amount / Math.Pow(1 + rate, years);
        }

        return npv;
    }
}
