using Purrfolio.Core.Enums;
using Purrfolio.Core.Models;
using Purrfolio.Core.Utilities;

namespace Purrfolio.Core.Tests;

public sealed class CouponCalendarGeneratorTests
{
    [Fact]
    public void GenerateUpcomingEvents_ReturnsCouponAndMaturityEvents()
    {
        var records = new[]
        {
            new InvestmentRecord
            {
                Id = 1,
                AssetClass = AssetClass.GovernmentBonds,
                Name = "Test Bond",
                TradeDate = new DateOnly(2025, 1, 1),
                Quantity = 100,
                UnitPrice = 100,
                CouponRate = 0.03m,
                CouponFrequency = CouponFrequency.SemiAnnual,
                MaturityDate = new DateOnly(2027, 1, 1)
            }
        };

        var events = CouponCalendarGenerator.GenerateUpcomingEvents(records, new DateOnly(2026, 1, 1), 18);

        Assert.NotEmpty(events);
        Assert.Contains(events, x => !x.IsMaturityEvent);
        Assert.Contains(events, x => x.IsMaturityEvent);
    }

    [Fact]
    public void GenerateUpcomingEvents_IgnoreNonBondRecords()
    {
        var records = new[]
        {
            new InvestmentRecord
            {
                Id = 2,
                AssetClass = AssetClass.Stocks,
                Name = "Stock",
                TradeDate = new DateOnly(2025, 1, 1),
                Quantity = 10,
                UnitPrice = 10
            }
        };

        var events = CouponCalendarGenerator.GenerateUpcomingEvents(records, new DateOnly(2026, 1, 1), 12);

        Assert.Empty(events);
    }
}
