using CommunityToolkit.Mvvm.ComponentModel;

namespace Purrfolio.App.Models;

public partial class OcrInvestmentDraft : ObservableObject
{
    public OcrInvestmentDraft(OcrInvestmentCandidate source)
    {
        Name = source.Name;
        AssetClass = source.AssetClass;
        TradeDate = source.TradeDate;
        QuantityText = source.Quantity.ToString("0.####");
        UnitPriceText = source.UnitPrice.ToString("0.####");
        FeesText = source.Fees.ToString("0.####");
        CouponRateText = source.CouponRate.ToString("0.######");
        CouponFrequency = source.CouponFrequency;
        MaturityDate = source.MaturityDate ?? string.Empty;
        AccruedInterestText = source.AccruedInterest.ToString("0.####");
        IsSpecialGovernmentBond = source.IsSpecialGovernmentBond;
        Confidence = source.Confidence;
    }

    [ObservableProperty]
    private bool isSelected = true;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string assetClass = "GovernmentBonds";

    [ObservableProperty]
    private string tradeDate = string.Empty;

    [ObservableProperty]
    private string quantityText = "0";

    [ObservableProperty]
    private string unitPriceText = "0";

    [ObservableProperty]
    private string feesText = "0";

    [ObservableProperty]
    private string couponRateText = "0";

    [ObservableProperty]
    private string couponFrequency = "SemiAnnual";

    [ObservableProperty]
    private string maturityDate = string.Empty;

    [ObservableProperty]
    private string accruedInterestText = "0";

    [ObservableProperty]
    private bool isSpecialGovernmentBond;

    [ObservableProperty]
    private double confidence;
}
