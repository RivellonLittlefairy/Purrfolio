using System.Collections.Specialized;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Purrfolio.App.ViewModels;
using Windows.Foundation;

namespace Purrfolio.App.Views;

public sealed partial class HomePage : Page
{
    private const double FullCircle = 359.999;
    private bool _isInitialized;

    public AssetViewModel ViewModel { get; }

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
        RedrawCharts();
    }

    private void ChartCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RedrawCharts();
    }

    private void OnChartDataChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RedrawCharts();
    }

    private void RedrawCharts()
    {
        DrawDonutChart();
        DrawTrendChart();
    }

    private void DrawDonutChart()
    {
        DonutCanvas.Children.Clear();

        var slices = ViewModel.DonutSlices.Where(x => x.Weight > 0).ToArray();
        if (slices.Length == 0 || DonutCanvas.ActualWidth < 40 || DonutCanvas.ActualHeight < 40)
        {
            return;
        }

        var width = DonutCanvas.ActualWidth;
        var height = DonutCanvas.ActualHeight;

        var size = Math.Min(width, height) - 6;
        var outerRadius = size / 2;
        var innerRadius = outerRadius * 0.58;
        var center = new Point(width / 2, height / 2);

        var startAngle = -90d;

        foreach (var slice in slices)
        {
            var sweepAngle = (double)(slice.Weight * 360m);
            if (sweepAngle <= 0)
            {
                continue;
            }

            if (sweepAngle >= 360)
            {
                sweepAngle = FullCircle;
            }

            var path = new Path
            {
                Fill = new SolidColorBrush(ParseColor(slice.ColorHex)),
                Data = CreateDonutSegment(center, outerRadius, innerRadius, startAngle, sweepAngle)
            };

            DonutCanvas.Children.Add(path);
            startAngle += sweepAngle;
        }

        var centerLabel = new TextBlock
        {
            Text = "资产配比",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Opacity = 0.85
        };

        DonutCanvas.Children.Add(centerLabel);
        Canvas.SetLeft(centerLabel, center.X - 28);
        Canvas.SetTop(centerLabel, center.Y - 10);
    }

    private void DrawTrendChart()
    {
        TrendCanvas.Children.Clear();

        var points = ViewModel.NetWorthPoints;
        if (points.Count < 2 || TrendCanvas.ActualWidth < 120 || TrendCanvas.ActualHeight < 120)
        {
            return;
        }

        var width = TrendCanvas.ActualWidth;
        var height = TrendCanvas.ActualHeight;

        const double marginLeft = 60;
        const double marginRight = 20;
        const double marginTop = 20;
        const double marginBottom = 35;

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
            var ratio = i / 4d;
            var y = marginTop + ratio * plotHeight;
            var value = maxY - (decimal)ratio * (maxY - minY);

            TrendCanvas.Children.Add(new Line
            {
                X1 = marginLeft,
                Y1 = y,
                X2 = marginLeft + plotWidth,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
                StrokeThickness = 1
            });

            var label = new TextBlock
            {
                Text = $"¥{value:N0}",
                FontSize = 11,
                Opacity = 0.8
            };
            TrendCanvas.Children.Add(label);
            Canvas.SetLeft(label, 4);
            Canvas.SetTop(label, y - 9);
        }

        DrawSeries(points.Select(p => p.NetWorth).ToArray(), Color.FromArgb(255, 74, 144, 226), null);
        DrawSeries(points.Select(p => p.Csi300Benchmark).ToArray(), Color.FromArgb(255, 230, 126, 34), new DoubleCollection { 5, 4 });
        DrawSeries(points.Select(p => p.CpiBenchmark).ToArray(), Color.FromArgb(255, 76, 175, 80), new DoubleCollection { 3, 3 });

        var startLabel = new TextBlock
        {
            Text = points[0].Date.ToString("yyyy-MM"),
            FontSize = 11,
            Opacity = 0.8
        };
        TrendCanvas.Children.Add(startLabel);
        Canvas.SetLeft(startLabel, marginLeft);
        Canvas.SetTop(startLabel, marginTop + plotHeight + 6);

        var endLabel = new TextBlock
        {
            Text = points[^1].Date.ToString("yyyy-MM"),
            FontSize = 11,
            Opacity = 0.8
        };
        TrendCanvas.Children.Add(endLabel);
        Canvas.SetLeft(endLabel, marginLeft + plotWidth - 58);
        Canvas.SetTop(endLabel, marginTop + plotHeight + 6);

        void DrawSeries(decimal[] values, Color color, DoubleCollection? dashArray)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            if (dashArray is not null)
            {
                polyline.StrokeDashArray = dashArray;
            }

            var xStep = values.Length > 1 ? plotWidth / (values.Length - 1) : plotWidth;

            for (var i = 0; i < values.Length; i++)
            {
                var ratio = (double)((values[i] - minY) / (maxY - minY));
                var x = marginLeft + i * xStep;
                var y = marginTop + (1 - ratio) * plotHeight;
                polyline.Points.Add(new Point(x, y));
            }

            TrendCanvas.Children.Add(polyline);
        }
    }

    private static Geometry CreateDonutSegment(Point center, double outerRadius, double innerRadius, double startAngle, double sweepAngle)
    {
        var endAngle = startAngle + sweepAngle;

        var outerStart = PolarToCartesian(center, outerRadius, startAngle);
        var outerEnd = PolarToCartesian(center, outerRadius, endAngle);
        var innerEnd = PolarToCartesian(center, innerRadius, endAngle);
        var innerStart = PolarToCartesian(center, innerRadius, startAngle);

        var figure = new PathFigure { StartPoint = outerStart, IsClosed = true };

        figure.Segments.Add(new ArcSegment
        {
            Point = outerEnd,
            Size = new Size(outerRadius, outerRadius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = sweepAngle > 180
        });

        figure.Segments.Add(new LineSegment { Point = innerEnd });

        figure.Segments.Add(new ArcSegment
        {
            Point = innerStart,
            Size = new Size(innerRadius, innerRadius),
            SweepDirection = SweepDirection.Counterclockwise,
            IsLargeArc = sweepAngle > 180
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    private static Point PolarToCartesian(Point center, double radius, double angleInDegrees)
    {
        var angle = Math.PI * angleInDegrees / 180.0;
        return new Point(
            center.X + radius * Math.Cos(angle),
            center.Y + radius * Math.Sin(angle));
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
