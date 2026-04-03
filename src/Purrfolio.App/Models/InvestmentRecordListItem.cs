namespace Purrfolio.App.Models;

public sealed record InvestmentRecordListItem(
    int Id,
    string AssetClassLabel,
    string Name,
    DateOnly TradeDate,
    decimal Quantity,
    decimal UnitPrice,
    decimal Fees,
    decimal MarketValue);
