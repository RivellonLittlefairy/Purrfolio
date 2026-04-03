namespace Purrfolio.App.Models;

public sealed record NetWorthPoint(
    DateOnly Date,
    decimal NetWorth,
    decimal Csi300Benchmark,
    decimal CpiBenchmark);
