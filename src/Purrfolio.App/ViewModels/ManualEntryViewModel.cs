using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Purrfolio.App.Models;
using Purrfolio.Core.Enums;
using Purrfolio.Core.Models;
using Purrfolio.Core.Services;

namespace Purrfolio.App.ViewModels;

public partial class ManualEntryViewModel(IInvestmentRepository investmentRepository) : ObservableObject
{
    private static readonly IReadOnlyList<EnumOption<AssetClass>> AssetClassOptionSource =
    [
        new(AssetClass.Stocks, "股票"),
        new(AssetClass.Gold, "黄金"),
        new(AssetClass.GovernmentBonds, "政府债券"),
        new(AssetClass.Cash, "现金")
    ];

    private static readonly IReadOnlyList<EnumOption<CouponFrequency>> CouponFrequencyOptionSource =
    [
        new(CouponFrequency.Annual, "年付"),
        new(CouponFrequency.SemiAnnual, "半年付"),
        new(CouponFrequency.Quarterly, "季付"),
        new(CouponFrequency.Monthly, "月付")
    ];

    public IReadOnlyList<EnumOption<AssetClass>> AssetClassOptions => AssetClassOptionSource;

    public IReadOnlyList<EnumOption<CouponFrequency>> CouponFrequencyOptions => CouponFrequencyOptionSource;

    [ObservableProperty]
    private EnumOption<AssetClass>? selectedAssetClass = AssetClassOptionSource[0];

    [ObservableProperty]
    private EnumOption<CouponFrequency>? selectedCouponFrequency = CouponFrequencyOptionSource[1];

    [ObservableProperty]
    private DateTimeOffset tradeDate = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset maturityDate = DateTimeOffset.Now.AddYears(30);

    [ObservableProperty]
    private string assetName = string.Empty;

    [ObservableProperty]
    private string quantityText = "1";

    [ObservableProperty]
    private string unitPriceText = "0";

    [ObservableProperty]
    private string feesText = "0";

    [ObservableProperty]
    private string accruedInterestText = "0";

    [ObservableProperty]
    private string couponRateText = "0.0285";

    [ObservableProperty]
    private bool isSpecialGovernmentBond;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private bool isStatusOpen;

    [ObservableProperty]
    private string statusTitle = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity statusSeverity = InfoBarSeverity.Informational;

    public bool ShowBondFields => SelectedAssetClass?.Value == AssetClass.GovernmentBonds;

    partial void OnSelectedAssetClassChanged(EnumOption<AssetClass>? value)
    {
        if (value?.Value != AssetClass.GovernmentBonds)
        {
            IsSpecialGovernmentBond = false;
        }

        OnPropertyChanged(nameof(ShowBondFields));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;

        try
        {
            if (!TryBuildRecord(out var record, out var validationError))
            {
                PublishStatus("输入校验失败", validationError, InfoBarSeverity.Warning);
                return;
            }

            await investmentRepository.AddInvestmentAsync(record);
            PublishStatus("保存成功", "投资记录已写入本地 SQLite。", InfoBarSeverity.Success);

            ResetAfterSave(record.AssetClass);
        }
        catch (Exception ex)
        {
            PublishStatus("保存失败", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool TryBuildRecord(out InvestmentRecord record, out string error)
    {
        record = new InvestmentRecord();
        error = string.Empty;

        if (SelectedAssetClass is null)
        {
            error = "请选择资产类别。";
            return false;
        }

        if (!TryParseNonNegativeDecimal(QuantityText, out var quantity) || quantity <= 0)
        {
            error = "数量必须是大于 0 的数值。";
            return false;
        }

        if (!TryParseNonNegativeDecimal(UnitPriceText, out var unitPrice))
        {
            error = "成交价格式不正确。";
            return false;
        }

        if (!TryParseNonNegativeDecimal(FeesText, out var fees))
        {
            error = "费用格式不正确。";
            return false;
        }

        decimal accruedInterest = 0m;
        decimal couponRate = 0m;
        DateOnly? maturityDate = null;

        if (ShowBondFields)
        {
            if (!TryParseNonNegativeDecimal(AccruedInterestText, out accruedInterest))
            {
                error = "应计利息格式不正确。";
                return false;
            }

            if (!TryParseNonNegativeDecimal(CouponRateText, out couponRate) || couponRate > 1m)
            {
                error = "票面利率应为 0 到 1 之间的小数（例如 0.0285）。";
                return false;
            }

            maturityDate = DateOnly.FromDateTime(MaturityDate.Date);
        }

        record = new InvestmentRecord
        {
            AssetClass = SelectedAssetClass.Value.Value,
            Name = string.IsNullOrWhiteSpace(AssetName) ? SelectedAssetClass.Value.Label : AssetName.Trim(),
            TradeDate = DateOnly.FromDateTime(TradeDate.Date),
            Quantity = quantity,
            UnitPrice = unitPrice,
            Fees = fees,
            IsSpecialGovernmentBond = ShowBondFields && IsSpecialGovernmentBond,
            CouponRate = ShowBondFields ? couponRate : 0m,
            CouponFrequency = ShowBondFields
                ? SelectedCouponFrequency?.Value ?? CouponFrequency.SemiAnnual
                : CouponFrequency.Annual,
            MaturityDate = maturityDate,
            AccruedInterest = ShowBondFields ? accruedInterest : 0m
        };

        return true;
    }

    private void ResetAfterSave(AssetClass assetClass)
    {
        AssetName = string.Empty;
        QuantityText = "1";
        UnitPriceText = "0";
        FeesText = "0";

        if (assetClass == AssetClass.GovernmentBonds)
        {
            AccruedInterestText = "0";
            CouponRateText = "0.0285";
            IsSpecialGovernmentBond = false;
        }
    }

    private void PublishStatus(string title, string message, InfoBarSeverity severity)
    {
        StatusTitle = title;
        StatusMessage = message;
        StatusSeverity = severity;
        IsStatusOpen = true;
    }

    private static bool TryParseNonNegativeDecimal(string input, out decimal value)
    {
        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0)
        {
            return true;
        }

        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value) && value >= 0)
        {
            return true;
        }

        value = 0;
        return false;
    }
}
