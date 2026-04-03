namespace Purrfolio.App.Models;

public sealed record CouponReminderItem(
    string BondName,
    DateOnly PayoutDate,
    int DaysLeft,
    decimal Amount,
    string EventType);
