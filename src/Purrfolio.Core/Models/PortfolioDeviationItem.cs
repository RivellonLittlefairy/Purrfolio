using Purrfolio.Core.Enums;

namespace Purrfolio.Core.Models;

public sealed record PortfolioDeviationItem(
    AssetClass AssetClass,
    decimal CurrentWeight,
    decimal TargetWeight,
    decimal Deviation,
    bool IsAlert);
