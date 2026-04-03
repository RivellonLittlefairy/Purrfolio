using Purrfolio.Core.Enums;
using Purrfolio.Core.Models;

namespace Purrfolio.Core.Utilities;

public static class CouponCalendarGenerator
{
    public static IReadOnlyList<CouponEvent> GenerateUpcomingEvents(
        IEnumerable<InvestmentRecord> bondRecords,
        DateOnly fromDate,
        int monthsAhead = 24)
    {
        if (monthsAhead <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthsAhead));
        }

        var endDate = fromDate.AddMonths(monthsAhead);
        var events = new List<CouponEvent>();

        foreach (var record in bondRecords.Where(r => r.AssetClass == AssetClass.GovernmentBonds))
        {
            var frequency = Math.Max(1, (int)record.CouponFrequency);
            var monthStep = Math.Max(1, 12 / frequency);
            var principal = record.Quantity * record.UnitPrice;

            if (record.CouponRate > 0 && principal > 0)
            {
                var couponAmount = principal * record.CouponRate / frequency;
                if (couponAmount > 0)
                {
                    var nextDate = record.TradeDate.AddMonths(monthStep);
                    var lastDate = record.MaturityDate is { } maturity && maturity < endDate ? maturity : endDate;

                    while (nextDate <= lastDate)
                    {
                        if (nextDate >= fromDate)
                        {
                            events.Add(new CouponEvent(record.Id, record.Name, nextDate, couponAmount, false));
                        }

                        nextDate = nextDate.AddMonths(monthStep);
                    }
                }
            }

            if (record.MaturityDate is { } maturityDate && maturityDate >= fromDate && maturityDate <= endDate)
            {
                events.Add(new CouponEvent(record.Id, record.Name, maturityDate, principal, true));
            }
        }

        return events
            .OrderBy(e => e.PayoutDate)
            .ThenBy(e => e.BondName)
            .ToArray();
    }
}
