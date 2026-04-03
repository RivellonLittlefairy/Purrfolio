namespace Purrfolio.App.Models;

public sealed record ProjectionPoint(
    int MonthIndex,
    DateOnly Date,
    decimal Balance);
