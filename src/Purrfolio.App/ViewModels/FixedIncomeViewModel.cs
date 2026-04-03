using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Purrfolio.App.Models;
using Purrfolio.App.Services;
using Purrfolio.Core.Enums;
using Purrfolio.Core.Models;
using Purrfolio.Core.Services;
using Purrfolio.Core.Utilities;

namespace Purrfolio.App.ViewModels;

public partial class FixedIncomeViewModel(
    IInvestmentRepository investmentRepository,
    INotificationService notificationService) : ObservableObject
{
    private readonly List<BondLotXirrItem> _allLots = [];

    public ObservableCollection<BondXirrSummaryItem> BondItems { get; } = [];

    public ObservableCollection<BondLotXirrItem> VisibleLots { get; } = [];

    public ObservableCollection<string> BondNames { get; } = [];
    public ObservableCollection<CouponReminderItem> UpcomingCoupons { get; } = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusText = "点击刷新载入固定收益数据。";

    [ObservableProperty]
    private string portfolioXirrText = "N/A";

    [ObservableProperty]
    private string selectedBondName = "全部";

    [ObservableProperty]
    private string couponCalendarHintText = "暂无派息提醒。";

    partial void OnSelectedBondNameChanged(string value)
    {
        ApplyLotFilter();
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private async Task PushCouponRemindersAsync()
    {
        var targetItems = UpcomingCoupons.Take(3).ToArray();
        if (targetItems.Length == 0)
        {
            await notificationService.NotifyAsync("Purrfolio 派息提醒", "未来 12 个月暂无派息事件。");
            return;
        }

        foreach (var item in targetItems)
        {
            await notificationService.NotifyAsync(
                "Purrfolio 派息提醒",
                $"{item.BondName} - {item.PayoutDate:yyyy-MM-dd} - {item.EventType} ¥{item.Amount:N2}");
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;

        try
        {
            BondItems.Clear();
            VisibleLots.Clear();
            BondNames.Clear();
            UpcomingCoupons.Clear();
            _allLots.Clear();

            var records = new List<InvestmentRecord>();
            await foreach (var record in investmentRepository.StreamInvestmentsAsync(cancellationToken))
            {
                if (record.AssetClass == AssetClass.GovernmentBonds)
                {
                    records.Add(record);
                }
            }

            if (records.Count == 0)
            {
                PortfolioXirrText = "N/A";
                StatusText = "暂无债券记录，请在“手动录入”页新增政府债券记录。";
                CouponCalendarHintText = "未来 18 个月暂无派息或到期事件。";
                return;
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var portfolioFlows = new List<CashFlow>();

            foreach (var record in records.OrderBy(r => r.TradeDate).ThenBy(r => r.Id))
            {
                var costBasis = record.Quantity * record.UnitPrice + record.Fees;
                var marketValue = record.Quantity * record.UnitPrice + record.AccruedInterest;

                var lotFlows = BuildLotCashFlows(record, today, costBasis, marketValue);
                portfolioFlows.AddRange(lotFlows);

                _allLots.Add(new BondLotXirrItem(
                    BondName: record.Name,
                    LotLabel: $"{record.TradeDate:yyyy-MM-dd} / 批次#{record.Id}",
                    TradeDate: record.TradeDate,
                    CostBasis: costBasis,
                    MarketValue: marketValue,
                    CouponRate: record.CouponRate,
                    IsSpecialGovernmentBond: record.IsSpecialGovernmentBond,
                    XirrDisplay: TryFormatXirr(lotFlows),
                    IsXirrAvailable: HasValidSigns(lotFlows)));
            }

            foreach (var group in _allLots.GroupBy(x => x.BondName).OrderBy(g => g.Key))
            {
                var groupRecords = records.Where(r => r.Name == group.Key).ToList();
                var groupFlows = groupRecords
                    .SelectMany(r =>
                    {
                        var costBasis = r.Quantity * r.UnitPrice + r.Fees;
                        var marketValue = r.Quantity * r.UnitPrice + r.AccruedInterest;
                        return BuildLotCashFlows(r, today, costBasis, marketValue);
                    })
                    .ToList();

                var totalCost = group.Sum(x => x.CostBasis);
                var totalMarketValue = group.Sum(x => x.MarketValue);

                BondItems.Add(new BondXirrSummaryItem(
                    BondName: group.Key,
                    LotsCount: group.Count(),
                    CostBasis: totalCost,
                    MarketValue: totalMarketValue,
                    Profit: totalMarketValue - totalCost,
                    XirrDisplay: TryFormatXirr(groupFlows),
                    IsXirrAvailable: HasValidSigns(groupFlows)));
            }

            BondNames.Add("全部");
            foreach (var name in BondItems.Select(x => x.BondName))
            {
                BondNames.Add(name);
            }

            SelectedBondName = BondNames[0];
            PortfolioXirrText = TryFormatXirr(portfolioFlows);
            StatusText = $"已载入 {records.Count} 笔债券记录，覆盖 {BondItems.Count} 只债券。";
            BuildCouponCalendar(records, today);
            ApplyLotFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildCouponCalendar(IReadOnlyCollection<InvestmentRecord> records, DateOnly today)
    {
        UpcomingCoupons.Clear();

        var events = CouponCalendarGenerator.GenerateUpcomingEvents(records, today, monthsAhead: 18);
        foreach (var evt in events.Take(18))
        {
            var daysLeft = evt.PayoutDate.DayNumber - today.DayNumber;
            UpcomingCoupons.Add(new CouponReminderItem(
                BondName: evt.BondName,
                PayoutDate: evt.PayoutDate,
                DaysLeft: daysLeft,
                Amount: evt.CouponAmount,
                EventType: evt.IsMaturityEvent ? "到期兑付" : "派息"));
        }

        CouponCalendarHintText = UpcomingCoupons.Count == 0
            ? "未来 18 个月暂无派息或到期事件。"
            : $"已生成未来 {UpcomingCoupons.Count} 条派息/兑付提醒。";
    }

    private void ApplyLotFilter()
    {
        VisibleLots.Clear();

        var targetLots = SelectedBondName == "全部"
            ? _allLots
            : _allLots.Where(x => x.BondName == SelectedBondName);

        foreach (var lot in targetLots.OrderByDescending(x => x.TradeDate))
        {
            VisibleLots.Add(lot);
        }
    }

    private static List<CashFlow> BuildLotCashFlows(InvestmentRecord record, DateOnly today, decimal costBasis, decimal marketValue)
    {
        var flows = new List<CashFlow>
        {
            new(record.TradeDate.ToDateTime(TimeOnly.MinValue), -costBasis)
        };

        var frequencyValue = Math.Max(1, (int)record.CouponFrequency);
        var monthStep = Math.Max(1, 12 / frequencyValue);

        if (record.CouponRate > 0 && monthStep > 0)
        {
            var couponAmount = record.Quantity * record.UnitPrice * record.CouponRate / frequencyValue;
            if (couponAmount > 0)
            {
                var couponDate = record.TradeDate.AddMonths(monthStep);
                var couponEndDate = record.MaturityDate is { } maturity && maturity < today ? maturity : today;

                while (couponDate <= couponEndDate)
                {
                    flows.Add(new CashFlow(couponDate.ToDateTime(TimeOnly.MinValue), couponAmount));
                    couponDate = couponDate.AddMonths(monthStep);
                }
            }
        }

        var terminalDate = record.MaturityDate is { } terminalMaturity && terminalMaturity < today
            ? terminalMaturity
            : today;

        flows.Add(new CashFlow(terminalDate.ToDateTime(TimeOnly.MinValue), Math.Max(marketValue, 0m)));

        return flows;
    }

    private static string TryFormatXirr(IReadOnlyCollection<CashFlow> cashFlows)
    {
        if (!HasValidSigns(cashFlows))
        {
            return "N/A";
        }

        try
        {
            var xirr = XirrCalculator.Calculate(cashFlows);
            return $"{xirr:P2}";
        }
        catch
        {
            return "N/A";
        }
    }

    private static bool HasValidSigns(IEnumerable<CashFlow> cashFlows)
    {
        var hasPositive = false;
        var hasNegative = false;

        foreach (var flow in cashFlows)
        {
            if (flow.Amount > 0)
            {
                hasPositive = true;
            }
            else if (flow.Amount < 0)
            {
                hasNegative = true;
            }

            if (hasPositive && hasNegative)
            {
                return true;
            }
        }

        return false;
    }
}
