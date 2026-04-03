namespace Purrfolio.App.Models;

public sealed record OcrInvestmentCandidate(
    string Name,
    string AssetClass,
    string TradeDate,
    decimal Quantity,
    decimal UnitPrice,
    decimal Fees,
    decimal CouponRate,
    string CouponFrequency,
    string? MaturityDate,
    decimal AccruedInterest,
    bool IsSpecialGovernmentBond,
    double Confidence);
