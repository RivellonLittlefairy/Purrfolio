namespace Purrfolio.App.Models;

public sealed record BondXirrSummaryItem(
    string BondName,
    int LotsCount,
    decimal CostBasis,
    decimal MarketValue,
    decimal Profit,
    string XirrDisplay,
    bool IsXirrAvailable);
