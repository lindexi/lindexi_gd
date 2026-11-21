using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace MathGraph.PostExpansionLearning.Wpf;

internal sealed class TrainingChart(LossGradient[] samples, bool showGradient, double bound) : FrameworkElement
{
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var plot = new Rect(52, 10, Math.Max(0, ActualWidth - 70), Math.Max(0, ActualHeight - 35));
        if (plot.Width == 0 || plot.Height == 0)
        {
            return;
        }

        double upper = bound > 0 ? bound : 1;
        double lower = showGradient ? -upper : 0;
        double X(int step) => plot.Left + plot.Width * step / (samples.Length - 1);
        double Y(double value) => plot.Bottom - plot.Height * (value - lower) / (upper - lower);

        var grid = new Pen(SystemColors.ActiveBorderBrush, 1);
        for (int i = 0; i <= 4; i++)
        {
            double value = lower + (upper - lower) * i / 4;
            double y = Y(value);
            drawingContext.DrawLine(grid, new Point(plot.Left, y), new Point(plot.Right, y));
            DrawLabel(value.ToString("G3", CultureInfo.CurrentCulture), plot.Left - 6, y, 1, 0.5);
        }

        int lastStep = samples.Length - 1;
        for (int i = 0; i <= 4; i++)
        {
            int step = lastStep * i / 4;
            double x = X(step);
            drawingContext.DrawLine(grid, new Point(x, plot.Top), new Point(x, plot.Bottom + 4));
            DrawLabel(step.ToString(CultureInfo.CurrentCulture), x, plot.Bottom + 5, 0.5, 0);
        }

        if (!showGradient)
        {
            DrawSeries(sample => sample.Loss, new Pen(Brushes.DarkBlue, 2));
        }
        else if (samples.All(sample => sample.Gradient == default))
        {
            DrawSeries(sample => sample.Gradient.Gate, new Pen(Brushes.DimGray, 2));
        }
        else
        {
            DrawSeries(sample => sample.Gradient.Gate, new Pen(Brushes.DarkBlue, 2));
            DrawSeries(sample => sample.Gradient.Positive, new Pen(Brushes.DarkRed, 2) { DashStyle = DashStyles.Dash });
            DrawSeries(sample => sample.Gradient.Negative, new Pen(Brushes.DarkGreen, 2) { DashStyle = DashStyles.Dot });
        }

        void DrawSeries(Func<LossGradient, double> select, Pen pen)
        {
            var geometry = new StreamGeometry();
            using (var figure = geometry.Open())
            {
                figure.BeginFigure(new Point(X(0), Y(select(samples[0]))), isFilled: false, isClosed: false);
                for (int step = 1; step < samples.Length; step++)
                {
                    figure.LineTo(new Point(X(step), Y(select(samples[step]))), isStroked: true, isSmoothJoin: false);
                }
            }

            drawingContext.DrawGeometry(null, pen, geometry);
        }

        void DrawLabel(string text, double x, double y, double horizontalAnchor, double verticalAnchor)
        {
            var label = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 11, SystemColors.ControlTextBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawingContext.DrawText(label, new Point(x - label.Width * horizontalAnchor, y - label.Height * verticalAnchor));
        }
    }
}
