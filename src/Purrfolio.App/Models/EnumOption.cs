namespace Purrfolio.App.Models;

public sealed record EnumOption<TEnum>(TEnum Value, string Label)
    where TEnum : struct, Enum;
