using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Purrfolio.App.Models;
using Purrfolio.Core.Utilities;

namespace Purrfolio.App.ViewModels;

public partial class ProjectionViewModel : ObservableObject
{
    public ObservableCollection<ProjectionPoint> ProjectionPoints { get; } = [];

    [ObservableProperty]
    private string currentBalanceText = "120000";

    [ObservableProperty]
    private string targetBalanceText = "1000000";

    [ObservableProperty]
    private string monthlyContributionText = "8000";

    [ObservableProperty]
    private string annualizedReturnRateText = "0.06";

    [ObservableProperty]
    private DateTimeOffset startDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string simulateResultText = "请点击“开始模拟”。";

    [ObservableProperty]
    private string progressText = "0%";

    [ObservableProperty]
    private double progressPercentage;

    [ObservableProperty]
    private bool isStatusOpen;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [RelayCommand]
    private void Simulate()
    {
        if (!TryParseInputs(out var currentBalance, out var targetBalance, out var monthlyContribution, out var annualizedReturnRate))
        {
            return;
        }

        try
        {
            ProjectionPoints.Clear();

            var fromDate = DateOnly.FromDateTime(StartDate.Date);
            var result = CompoundProjectionCalculator.ProjectToTarget(
                currentBalance,
                targetBalance,
                monthlyContribution,
                annualizedReturnRate,
                fromDate);

            var monthlyRate = (decimal)Math.Pow((double)(1 + annualizedReturnRate), 1.0 / 12.0) - 1;
            var balance = currentBalance;

            ProjectionPoints.Add(new ProjectionPoint(0, fromDate, balance));
            for (var month = 1; month <= result.MonthsRequired; month++)
            {
                balance = (balance + monthlyContribution) * (1 + monthlyRate);
                ProjectionPoints.Add(new ProjectionPoint(month, fromDate.AddMonths(month), balance));
            }

            var percentage = targetBalance <= 0 ? 0 : (double)Math.Min(100m, currentBalance / targetBalance * 100m);
            ProgressPercentage = percentage;
            ProgressText = $"{percentage:0.00}%";

            SimulateResultText = $"预计 {result.MonthsRequired} 个月后达成目标（{result.ReachTargetDate:yyyy-MM-dd}），期末约 ¥{result.FinalProjectedBalance:N2}";
            IsStatusOpen = false;
        }
        catch (Exception ex)
        {
            SimulateResultText = "模拟失败";
            StatusMessage = ex.Message;
            IsStatusOpen = true;
        }
    }

    private bool TryParseInputs(
        out decimal currentBalance,
        out decimal targetBalance,
        out decimal monthlyContribution,
        out decimal annualizedReturnRate)
    {
        currentBalance = 0;
        targetBalance = 0;
        monthlyContribution = 0;
        annualizedReturnRate = 0;

        if (!TryParseDecimal(CurrentBalanceText, out currentBalance) || currentBalance < 0)
        {
            ShowValidationError("当前净值格式不正确。", CurrentBalanceText);
            return false;
        }

        if (!TryParseDecimal(TargetBalanceText, out targetBalance) || targetBalance <= 0)
        {
            ShowValidationError("目标金额必须大于 0。", TargetBalanceText);
            return false;
        }

        if (!TryParseDecimal(MonthlyContributionText, out monthlyContribution) || monthlyContribution < 0)
        {
            ShowValidationError("月投入格式不正确。", MonthlyContributionText);
            return false;
        }

        if (!TryParseDecimal(AnnualizedReturnRateText, out annualizedReturnRate) || annualizedReturnRate <= -1)
        {
            ShowValidationError("预期年化应大于 -1（例如 0.06）。", AnnualizedReturnRateText);
            return false;
        }

        return true;
    }

    private void ShowValidationError(string message, string input)
    {
        SimulateResultText = "模拟失败";
        StatusMessage = $"{message} 输入值：{input}";
        IsStatusOpen = true;
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
