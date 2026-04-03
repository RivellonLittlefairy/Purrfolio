namespace Purrfolio.App.Models;

public sealed record AssetDonutSlice(
    string Name,
    decimal Value,
    decimal Weight,
    string ColorHex);
