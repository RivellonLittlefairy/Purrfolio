using Purrfolio.Core.Enums;

namespace Purrfolio.Core.Models;

public sealed class InvestmentRecord
{
    public int Id { get; set; }

    public AssetClass AssetClass { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly TradeDate { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Fees { get; set; }

    public bool IsSpecialGovernmentBond { get; set; }

    public decimal CouponRate { get; set; }

    public CouponFrequency CouponFrequency { get; set; } = CouponFrequency.Annual;

    public DateOnly? MaturityDate { get; set; }

    public decimal AccruedInterest { get; set; }
}
