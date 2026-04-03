using Purrfolio.Core.Enums;
using Purrfolio.Core.Models;

namespace Purrfolio.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (dbContext.InvestmentRecords.Any())
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        var seedData = new List<InvestmentRecord>
        {
            new()
            {
                AssetClass = AssetClass.Stocks,
                Name = "沪深300ETF",
                TradeDate = today.AddMonths(-12),
                Quantity = 500,
                UnitPrice = 3.92m,
                Fees = 8m
            },
            new()
            {
                AssetClass = AssetClass.Gold,
                Name = "黄金ETF",
                TradeDate = today.AddMonths(-9),
                Quantity = 200,
                UnitPrice = 5.18m,
                Fees = 5m
            },
            new()
            {
                AssetClass = AssetClass.GovernmentBonds,
                Name = "30年特别国债 2025",
                TradeDate = today.AddMonths(-6),
                Quantity = 100,
                UnitPrice = 102.35m,
                IsSpecialGovernmentBond = true,
                CouponRate = 0.0285m,
                CouponFrequency = CouponFrequency.SemiAnnual,
                MaturityDate = today.AddYears(30),
                Fees = 0.5m,
                AccruedInterest = 88.4m
            },
            new()
            {
                AssetClass = AssetClass.Cash,
                Name = "活期现金",
                TradeDate = today,
                Quantity = 1,
                UnitPrice = 35000m,
                Fees = 0m
            }
        };

        await dbContext.InvestmentRecords.AddRangeAsync(seedData, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
