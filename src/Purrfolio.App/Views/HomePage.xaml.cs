using System.Collections.Specialized;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Purrfolio.App.Interfaces;
using Purrfolio.App.ViewModels;
using Windows.UI;

namespace Purrfolio.App.Views;

public sealed partial class HomePage : Page, IConnectedAnimationPage
{
    private static readonly CanvasTextFormat AxisTextFormat = new()
    {
        FontSize = 11
    };

    private bool _isInitialized;

    public AssetViewModel ViewModel { get; }
    public UIElement AnimationTarget => PageTitleText;

    public HomePage(AssetViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;

        ViewModel.DonutSlices.CollectionChanged += OnChartDataChanged;
        ViewModel.NetWorthPoints.CollectionChanged += OnChartDataChanged;
    }

    private async void HomePage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        await ViewModel.LoadAsync();
        InvalidateCharts();
    }

    private void ChartCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        InvalidateCharts();
    }

    private void OnChartDataChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateCharts();
    }

    private void InvalidateCharts()
    {
        DonutCanvas.Invalidate();
        TrendCanvas.Invalidate();
    }

    private void DonutCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var drawingSession = args.DrawingSession;
        drawingSession.Clear(Color.FromArgb(0, 0, 0, 0));

        var slices = ViewModel.DonutSlices.Where(x => x.Weight > 0).ToArray();
        if (slices.Length == 0 || sender.ActualWidth < 40 || sender.ActualHeight < 40)
        {
            return;
        }

        var width = (float)sender.ActualWidth;
        var height = (float)sender.ActualHeight;

        var size = MathF.Min(width, height) - 8f;
        var outerRadius = size / 2f;
        var innerRadius = outerRadius * 0.58f;
        var strokeWidth = outerRadius - innerRadius;
        var drawRadius = innerRadius + strokeWidth / 2f;

        var center = new Vector2(width / 2f, height / 2f);
        var startAngle = -MathF.PI / 2f;

        foreach (var slice in slices)
        {
            var sweep = Math.Max(0f, (float)(slice.Weight * (decimal)(2 * Math.PI)));
            if (sweep <= 0f)
            {
                continue;
            }

            if (sweep >= 2 * MathF.PI)
            {
                drawingSession.DrawCircle(center, drawRadius, ParseColor(slice.ColorHex), strokeWidth);
            }
            else
            {
                drawingSession.DrawArc(
                    center,
                    drawRadius,
                    drawRadius,
                    startAngle,
                    sweep,
                    ParseColor(slice.ColorHex),
                    strokeWidth);
            }

            startAngle += sweep;
        }

        drawingSession.DrawText("资产配比", new Vector2(center.X - 28f, center.Y - 8f), Color.FromArgb(220, 255, 255, 255));
    }

    private void TrendCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var drawingSession = args.DrawingSession;
        drawingSession.Clear(Color.FromArgb(0, 0, 0, 0));

        var points = ViewModel.NetWorthPoints;
        if (points.Count < 2 || sender.ActualWidth < 120 || sender.ActualHeight < 120)
        {
            return;
        }

        var width = (float)sender.ActualWidth;
        var height = (float)sender.ActualHeight;

        const float marginLeft = 60f;
        const float marginRight = 20f;
        const float marginTop = 20f;
        const float marginBottom = 35f;

        var plotWidth = width - marginLeft - marginRight;
        var plotHeight = height - marginTop - marginBottom;

        if (plotWidth <= 0 || plotHeight <= 0)
        {
            return;
        }

        var minY = points.Min(x => Math.Min(x.NetWorth, Math.Min(x.Csi300Benchmark, x.CpiBenchmark)));
        var maxY = points.Max(x => Math.Max(x.NetWorth, Math.Max(x.Csi300Benchmark, x.CpiBenchmark)));
        if (maxY <= minY)
        {
            maxY = minY + 1;
        }

        for (var i = 0; i <= 4; i++)
        {
            var ratio = i / 4f;
            var y = marginTop + ratio * plotHeight;
            var value = maxY - (decimal)ratio * (maxY - minY);

            drawingSession.DrawLine(
                new Vector2(marginLeft, y),
                new Vector2(marginLeft + plotWidth, y),
                Color.FromArgb(70, 255, 255, 255),
                1f);

            drawingSession.DrawText($"¥{value:N0}", new Vector2(4f, y - 8f), Color.FromArgb(215, 255, 255, 255), AxisTextFormat);
        }

        DrawSeries(
            drawingSession,
            points.Select(p => p.NetWorth).ToArray(),
            minY,
            maxY,
            marginLeft,
            marginTop,
            plotWidth,
            plotHeight,
            Color.FromArgb(255, 74, 144, 226));

        DrawSeries(
            drawingSession,
            points.Select(p => p.Csi300Benchmark).ToArray(),
            minY,
            maxY,
            marginLeft,
            marginTop,
            plotWidth,
            plotHeight,
            Color.FromArgb(255, 230, 126, 34),
            new float[] { 5f, 4f });

        DrawSeries(
            drawingSession,
            points.Select(p => p.CpiBenchmark).ToArray(),
            minY,
            maxY,
            marginLeft,
            marginTop,
            plotWidth,
            plotHeight,
            Color.FromArgb(255, 76, 175, 80),
            new float[] { 3f, 3f });

        drawingSession.DrawText(points[0].Date.ToString("yyyy-MM"), new Vector2(marginLeft, marginTop + plotHeight + 6), Color.FromArgb(215, 255, 255, 255), AxisTextFormat);
        drawingSession.DrawText(points[^1].Date.ToString("yyyy-MM"), new Vector2(marginLeft + plotWidth - 58, marginTop + plotHeight + 6), Color.FromArgb(215, 255, 255, 255), AxisTextFormat);
    }

    private static void DrawSeries(
        CanvasDrawingSession drawingSession,
        IReadOnlyList<decimal> values,
        decimal minY,
        decimal maxY,
        float marginLeft,
        float marginTop,
        float plotWidth,
        float plotHeight,
        Color color,
        float[]? dashPattern = null)
    {
        if (values.Count < 2)
        {
            return;
        }

        var strokeStyle = dashPattern is null
            ? null
            : new CanvasStrokeStyle
            {
                DashStyle = CanvasDashStyle.Custom,
                CustomDashStyle = dashPattern
            };

        var xStep = plotWidth / (values.Count - 1);

        for (var i = 1; i < values.Count; i++)
        {
            var previousRatio = (float)((values[i - 1] - minY) / (maxY - minY));
            var currentRatio = (float)((values[i] - minY) / (maxY - minY));

            var p1 = new Vector2(
                marginLeft + (i - 1) * xStep,
                marginTop + (1f - previousRatio) * plotHeight);
            var p2 = new Vector2(
                marginLeft + i * xStep,
                marginTop + (1f - currentRatio) * plotHeight);

            if (strokeStyle is null)
            {
                drawingSession.DrawLine(p1, p2, color, 2f);
            }
            else
            {
                drawingSession.DrawLine(p1, p2, color, 2f, strokeStyle);
            }
        }
    }

    private static Color ParseColor(string colorHex)
    {
        var hex = colorHex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }

        if (hex.Length == 8)
        {
            return Color.FromArgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16),
                Convert.ToByte(hex.Substring(6, 2), 16));
        }

        return Colors.Gray;
    }
}
