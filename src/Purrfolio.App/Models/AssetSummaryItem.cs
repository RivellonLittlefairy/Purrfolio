namespace Purrfolio.App.Models;

public sealed record AssetSummaryItem(
    string Name,
    decimal CurrentValue,
    decimal CurrentWeight,
    decimal TargetWeight,
    decimal Deviation,
    bool IsDeviationAlert);
