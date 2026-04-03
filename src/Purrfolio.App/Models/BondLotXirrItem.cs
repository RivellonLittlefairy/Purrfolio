namespace Purrfolio.App.Models;

public sealed record BondLotXirrItem(
    string BondName,
    string LotLabel,
    DateOnly TradeDate,
    decimal CostBasis,
    decimal MarketValue,
    decimal CouponRate,
    bool IsSpecialGovernmentBond,
    string XirrDisplay,
    bool IsXirrAvailable);
