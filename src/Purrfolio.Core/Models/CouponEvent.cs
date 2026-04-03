namespace Purrfolio.Core.Models;

public sealed record CouponEvent(
    int InvestmentRecordId,
    string BondName,
    DateOnly PayoutDate,
    decimal CouponAmount,
    bool IsMaturityEvent);
