using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Purrfolio.App.Models;
using Purrfolio.App.Services;
using Purrfolio.Core.Enums;
using Purrfolio.Core.Models;
using Purrfolio.Core.Services;

namespace Purrfolio.App.ViewModels;

public partial class OcrImportViewModel(
    IMinimaxOcrService minimaxOcrService,
    IInvestmentRepository investmentRepository) : ObservableObject
{
    private const string DefaultEndpoint = "https://api.minimax.chat/v1/text/chatcompletion_v2";
    private const string DefaultModel = "MiniMax-Text-01";

    public ObservableCollection<OcrInvestmentDraft> Drafts { get; } = [];

    [ObservableProperty]
    private string imagePath = string.Empty;

    [ObservableProperty]
    private string endpoint = GetEnvironmentOrDefault("MINIMAX_OCR_ENDPOINT", DefaultEndpoint);

    [ObservableProperty]
    private string apiKey = GetEnvironmentOrDefault("MINIMAX_API_KEY", string.Empty);

    [ObservableProperty]
    private string model = GetEnvironmentOrDefault("MINIMAX_MODEL", DefaultModel);

    [ObservableProperty]
    private bool useBearerToken = true;

    [ObservableProperty]
    private bool isAnalyzing;

    [ObservableProperty]
    private bool isImporting;

    [ObservableProperty]
    private bool isStatusOpen;

    [ObservableProperty]
    private string statusTitle = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity statusSeverity = InfoBarSeverity.Informational;

    public bool IsBusy => IsAnalyzing || IsImporting;

    partial void OnIsAnalyzingChanged(bool value) => OnPropertyChanged(nameof(IsBusy));

    partial void OnIsImportingChanged(bool value) => OnPropertyChanged(nameof(IsBusy));

    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        if (IsAnalyzing)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ImagePath))
        {
            PublishStatus("参数不完整", "请先选择截图文件。", InfoBarSeverity.Warning);
            return;
        }

        IsAnalyzing = true;

        try
        {
            var options = new MinimaxRequestOptions(
                Endpoint: Endpoint.Trim(),
                ApiKey: ApiKey.Trim(),
                Model: string.IsNullOrWhiteSpace(Model) ? DefaultModel : Model.Trim(),
                UseBearerToken: UseBearerToken);

            var candidates = await minimaxOcrService.AnalyzeScreenshotAsync(ImagePath.Trim(), options);

            Drafts.Clear();
            foreach (var candidate in candidates.OrderByDescending(x => x.Confidence))
            {
                Drafts.Add(new OcrInvestmentDraft(candidate));
            }

            PublishStatus(
                "识别完成",
                $"已提取 {Drafts.Count} 条候选记录，请核对后导入。",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            PublishStatus("识别失败", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    [RelayCommand]
    private async Task ImportSelectedAsync()
    {
        if (IsImporting)
        {
            return;
        }

        var selectedDrafts = Drafts.Where(x => x.IsSelected).ToList();
        if (selectedDrafts.Count == 0)
        {
            PublishStatus("无可导入项", "请至少勾选一条识别草稿。", InfoBarSeverity.Warning);
            return;
        }

        IsImporting = true;

        var importedDrafts = new List<OcrInvestmentDraft>();
        var errors = new List<string>();

        try
        {
            foreach (var draft in selectedDrafts)
            {
                if (!TryBuildRecord(draft, out var record, out var error))
                {
                    errors.Add($"{draft.Name}: {error}");
                    continue;
                }

                await investmentRepository.AddInvestmentAsync(record);
                importedDrafts.Add(draft);
            }

            foreach (var imported in importedDrafts)
            {
                Drafts.Remove(imported);
            }

            if (errors.Count > 0)
            {
                var first = errors[0];
                PublishStatus(
                    "部分导入成功",
                    $"成功 {importedDrafts.Count} 条，失败 {errors.Count} 条。示例：{first}",
                    InfoBarSeverity.Warning);
                return;
            }

            PublishStatus("导入成功", $"成功导入 {importedDrafts.Count} 条记录。", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            PublishStatus("导入失败", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var draft in Drafts)
        {
            draft.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var draft in Drafts)
        {
            draft.IsSelected = false;
        }
    }

    private bool TryBuildRecord(OcrInvestmentDraft draft, out InvestmentRecord record, out string error)
    {
        record = new InvestmentRecord();
        error = string.Empty;

        if (!TryParseAssetClass(draft.AssetClass, out var assetClass))
        {
            error = "资产类别无法识别，请改为 Stocks/Gold/GovernmentBonds/Cash。";
            return false;
        }

        if (!TryParseDateOnly(draft.TradeDate, out var tradeDate))
        {
            error = "交易日格式错误，请使用 yyyy-MM-dd。";
            return false;
        }

        if (!TryParseNonNegativeDecimal(draft.QuantityText, out var quantity) || quantity <= 0)
        {
            error = "数量必须为大于 0 的数值。";
            return false;
        }

        if (!TryParseNonNegativeDecimal(draft.UnitPriceText, out var unitPrice))
        {
            error = "成交价格式不正确。";
            return false;
        }

        if (!TryParseNonNegativeDecimal(draft.FeesText, out var fees))
        {
            error = "费用格式不正确。";
            return false;
        }

        var couponRate = 0m;
        var accruedInterest = 0m;
        DateOnly? maturityDate = null;
        var couponFrequency = CouponFrequency.Annual;

        if (assetClass == AssetClass.GovernmentBonds)
        {
            if (!TryParseNonNegativeDecimal(draft.CouponRateText, out couponRate) || couponRate > 1m)
            {
                error = "票面利率需在 0~1 之间。";
                return false;
            }

            if (!TryParseCouponFrequency(draft.CouponFrequency, out couponFrequency))
            {
                error = "派息频率无法识别，请改为 Annual/SemiAnnual/Quarterly/Monthly。";
                return false;
            }

            if (!TryParseNonNegativeDecimal(draft.AccruedInterestText, out accruedInterest))
            {
                error = "应计利息格式不正确。";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(draft.MaturityDate))
            {
                if (!TryParseDateOnly(draft.MaturityDate, out var parsedMaturity))
                {
                    error = "到期日格式错误，请使用 yyyy-MM-dd。";
                    return false;
                }

                maturityDate = parsedMaturity;
            }
        }

        record = new InvestmentRecord
        {
            AssetClass = assetClass,
            Name = string.IsNullOrWhiteSpace(draft.Name) ? ToAssetClassLabel(assetClass) : draft.Name.Trim(),
            TradeDate = tradeDate,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Fees = fees,
            CouponRate = assetClass == AssetClass.GovernmentBonds ? couponRate : 0m,
            CouponFrequency = assetClass == AssetClass.GovernmentBonds ? couponFrequency : CouponFrequency.Annual,
            MaturityDate = maturityDate,
            AccruedInterest = assetClass == AssetClass.GovernmentBonds ? accruedInterest : 0m,
            IsSpecialGovernmentBond = assetClass == AssetClass.GovernmentBonds && draft.IsSpecialGovernmentBond
        };

        return true;
    }

    private void PublishStatus(string title, string message, InfoBarSeverity severity)
    {
        StatusTitle = title;
        StatusMessage = message;
        StatusSeverity = severity;
        IsStatusOpen = true;
    }

    private static bool TryParseDateOnly(string value, out DateOnly date)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsedDateTime))
        {
            date = DateOnly.FromDateTime(parsedDateTime);
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
        {
            date = DateOnly.FromDateTime(parsedDateTime);
            return true;
        }

        date = default;
        return false;
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

    private static bool TryParseAssetClass(string input, out AssetClass assetClass)
    {
        var token = NormalizeToken(input);

        assetClass = token switch
        {
            "stocks" or "stock" or "股票" or "权益" => AssetClass.Stocks,
            "gold" or "黄金" => AssetClass.Gold,
            "governmentbonds" or "governmentbond" or "govbond" or "govbonds" or "bond" or "bonds" or "债券" or "国债" or "政府债券" or "特别国债" or "超长期特别国债" => AssetClass.GovernmentBonds,
            "cash" or "现金" or "货币" => AssetClass.Cash,
            _ => (AssetClass)(-1)
        };

        return Enum.IsDefined(assetClass);
    }

    private static bool TryParseCouponFrequency(string input, out CouponFrequency frequency)
    {
        var token = NormalizeToken(input);

        frequency = token switch
        {
            "annual" or "yearly" or "年付" or "每年" => CouponFrequency.Annual,
            "semiannual" or "semiannually" or "halfyearly" or "半年付" or "每半年" => CouponFrequency.SemiAnnual,
            "quarterly" or "季付" or "每季" => CouponFrequency.Quarterly,
            "monthly" or "月付" or "每月" => CouponFrequency.Monthly,
            _ => (CouponFrequency)(-1)
        };

        return Enum.IsDefined(frequency);
    }

    private static string NormalizeToken(string input)
    {
        return (input ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);
    }

    private static string ToAssetClassLabel(AssetClass assetClass)
    {
        return assetClass switch
        {
            AssetClass.Stocks => "股票",
            AssetClass.Gold => "黄金",
            AssetClass.GovernmentBonds => "政府债券",
            AssetClass.Cash => "现金",
            _ => assetClass.ToString()
        };
    }

    private static string GetEnvironmentOrDefault(string name, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
