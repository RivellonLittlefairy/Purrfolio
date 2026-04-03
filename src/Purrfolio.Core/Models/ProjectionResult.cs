namespace Purrfolio.Core.Models;

public sealed record ProjectionResult(
    int MonthsRequired,
    DateOnly ReachTargetDate,
    decimal FinalProjectedBalance);
