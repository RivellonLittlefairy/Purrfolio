using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Purrfolio.App.Models;
using Purrfolio.Core.Enums;
using Purrfolio.Core.Models;
using Purrfolio.Core.Services;
using Purrfolio.Core.Utilities;

namespace Purrfolio.App.ViewModels;

public partial class AssetViewModel(IInvestmentRepository investmentRepository) : ObservableObject
{
    private readonly Dictionary<AssetClass, decimal> _allocationMap = new();

    public ObservableCollection<AssetSummaryItem> AssetItems { get; } = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private decimal totalNetWorth;

    [ObservableProperty]
    private bool hasDeviationWarning;

    [ObservableProperty]
    private string deviationWarningText = "当前资产配比符合永久组合目标。";

    [ObservableProperty]
    private double goalProgressPercentage;

    [ObservableProperty]
    private string goalProgressText = "0%";

    [ObservableProperty]
    private string projectionText = "尚未计算";

    [ObservableProperty]
    private string countdownText = string.Empty;

    [ObservableProperty]
    private string bondPortfolioXirrText = "N/A";

    public decimal GoalTargetAmount { get; set; } = 1_000_000m;

    public decimal ExpectedAnnualizedReturn { get; set; } = 0.06m;

    public decimal MonthlyContribution { get; set; } = 8_000m;

    public DateOnly BirthDate { get; set; } = new(1998, 10, 1);

    public int WealthFreedomAge { get; set; } = 30;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;

        try
        {
            _allocationMap.Clear();
            AssetItems.Clear();

            var bondCashFlows = new List<CashFlow>();
            var today = DateTime.Today;

            await foreach (var record in investmentRepository.StreamInvestmentsAsync(cancellationToken))
            {
                var marketValue = record.Quantity * record.UnitPrice + record.AccruedInterest - record.Fees;
                _allocationMap[record.AssetClass] = _allocationMap.GetValueOrDefault(record.AssetClass) + marketValue;

                if (record.AssetClass == AssetClass.GovernmentBonds)
                {
                    var buyAmount = -(record.Quantity * record.UnitPrice + record.Fees);
                    bondCashFlows.Add(new CashFlow(record.TradeDate.ToDateTime(TimeOnly.MinValue), buyAmount));
                }
            }

            TotalNetWorth = _allocationMap.Values.Sum();

            var deviations = PermanentPortfolioAnalyzer.Analyze(_allocationMap, 0.05m);
            foreach (var item in deviations)
            {
                _allocationMap.TryGetValue(item.AssetClass, out var currentValue);

                AssetItems.Add(new AssetSummaryItem(
                    Name: ToDisplayName(item.AssetClass),
                    CurrentValue: currentValue,
                    CurrentWeight: item.CurrentWeight,
                    TargetWeight: item.TargetWeight,
                    Deviation: item.Deviation,
                    IsDeviationAlert: item.IsAlert));
            }

            var warningItems = deviations.Where(x => x.IsAlert).ToArray();
            HasDeviationWarning = warningItems.Length > 0;
            DeviationWarningText = warningItems.Length > 0
                ? $"偏离超过 5%：{string.Join("、", warningItems.Select(x => ToDisplayName(x.AssetClass)))}"
                : "当前资产配比符合永久组合目标。";

            GoalProgressPercentage = GoalTargetAmount <= 0
                ? 0
                : (double)Math.Min(100m, TotalNetWorth / GoalTargetAmount * 100m);
            GoalProgressText = $"{GoalProgressPercentage:0.00}%";

            var projection = CompoundProjectionCalculator.ProjectToTarget(
                TotalNetWorth,
                GoalTargetAmount,
                MonthlyContribution,
                ExpectedAnnualizedReturn,
                DateOnly.FromDateTime(today));

            ProjectionText = $"按当前节奏预计 {projection.MonthsRequired} 个月后达成（{projection.ReachTargetDate:yyyy-MM-dd}）";

            var targetBirthday = BirthDate.AddYears(WealthFreedomAge);
            var daysLeft = targetBirthday.DayNumber - DateOnly.FromDateTime(today).DayNumber;
            CountdownText = daysLeft >= 0
                ? $"距离 {WealthFreedomAge} 岁还有 {daysLeft} 天"
                : $"已超过 {WealthFreedomAge} 岁 {-daysLeft} 天";

            if (_allocationMap.TryGetValue(AssetClass.GovernmentBonds, out var bondValue) && bondValue > 0)
            {
                bondCashFlows.Add(new CashFlow(today, bondValue));
                try
                {
                    var xirr = XirrCalculator.Calculate(bondCashFlows);
                    BondPortfolioXirrText = $"{xirr:P2}";
                }
                catch (InvalidOperationException)
                {
                    BondPortfolioXirrText = "N/A";
                }
            }
            else
            {
                BondPortfolioXirrText = "N/A";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    private static string ToDisplayName(AssetClass assetClass)
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
}
