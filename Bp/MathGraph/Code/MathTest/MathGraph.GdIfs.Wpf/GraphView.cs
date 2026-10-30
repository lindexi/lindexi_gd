using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MathGraph.GdIfs.Wpf.Model;

namespace MathGraph.GdIfs.Wpf;

/// <summary>Draws the pure model with screen-space hit targets and a zoomable viewport.</summary>
public sealed class GraphView : FrameworkElement
{
    private static readonly Brush[] Palette =
    [Brushes.Turquoise, Brushes.CornflowerBlue, Brushes.Plum, Brushes.Gold, Brushes.Coral,
     Brushes.LightGreen, Brushes.DeepSkyBlue, Brushes.HotPink, Brushes.Wheat];
    private CycleModel _model = CycleModel.Create(0, 3, 0.28);
    private double _zoom = 1;
    private Vector _pan;
    private Point? _drag;
    private bool _moved;

    /// <summary>Raised when a local vertex is selected at any visible level.</summary>
    public event Action<int>? NodeSelected;

    /// <summary>The model to render; setting it preserves the viewport.</summary>
    public CycleModel Model
    {
        get => _model;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _model = value;
            InvalidateVisual();
        }
    }

    private double Scale => Math.Max(1, Math.Min(ActualWidth, ActualHeight) * 0.28) * _zoom;
    private Point Origin => new(ActualWidth / 2 + _pan.X, ActualHeight / 2 + _pan.Y);
    private Point Screen(GraphPoint point) => new(Origin.X + point.X * Scale, Origin.Y + point.Y * Scale);

    /// <summary>Restores the initial overview.</summary>
    public void ResetView()
    {
        _zoom = 1;
        _pan = default;
        InvalidateVisual();
    }

    /// <summary>Zooms around the viewport center.</summary>
    public void Zoom(double factor) => ZoomAt(factor, new Point(ActualWidth / 2, ActualHeight / 2));

    private void ZoomAt(double factor, Point anchor)
    {
        if (!double.IsFinite(factor) || factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
        double next = Math.Clamp(_zoom * factor, 0.3, 100_000_000);
        double ratio = next / _zoom;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        _pan = (anchor - center) - (anchor - Origin) * ratio;
        _zoom = next;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.PushClip(new RectangleGeometry(new Rect(RenderSize)));
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        foreach (var ring in Model.Rings)
        {
            var brush = Palette[ring.Level % Palette.Length];
            var pen = new Pen(brush, 1.7);
            if (ring.Level < Model.Rings.Count - 1)
            {
                var containerPen = new Pen(brush, 1) { DashStyle = DashStyles.Dash };
                double radius = ring.Radius * Model.Contraction * Scale * 1.13;
                dc.DrawEllipse(null, containerPen, Screen(ring.Nodes[Model.SelectedNode]), radius, radius);
            }
            foreach (var edge in CycleModel.Edges)
                dc.DrawLine(pen, Screen(ring.Nodes[edge.From]), Screen(ring.Nodes[edge.To]));
            for (int index = 0; index < 5; index++)
            {
                Point point = Screen(ring.Nodes[index]);
                bool selected = index == Model.SelectedNode;
                dc.DrawEllipse(selected ? brush : new SolidColorBrush(Color.FromRgb(16, 28, 46)), pen, point, selected ? 6 : 4, selected ? 6 : 4);
                if (ring.Radius * Scale < 28) continue;
                var label = new FormattedText(
                    string.Format(CultureInfo.CurrentCulture, (string)FindResource("LevelFormat"), ring.Level, index + 1),
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 12, brush,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(label, new Point(point.X + 9, point.Y + 6));
            }
        }
        dc.Pop();
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        ZoomAt(e.Delta > 0 ? 1.4 : 1 / 1.4, e.GetPosition(this));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _drag = e.GetPosition(this);
        _moved = false;
        CaptureMouse();
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_drag is not { } previous) return;
        Point current = e.GetPosition(this);
        Vector delta = current - previous;
        if (!_moved && delta.Length < 4) return;
        _moved = true;
        _pan += delta;
        _drag = current;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_drag is null) return;
        _drag = null;
        ReleaseMouseCapture();
        if (_moved) return;
        Point click = e.GetPosition(this);
        // Fixed screen-space targets stay usable even when a ring is very small.
        int? nearest = null;
        double distance = 14;
        foreach (var ring in Model.Rings)
        {
            for (int index = 0; index < 5; index++)
            {
                double candidate = (Screen(ring.Nodes[index]) - click).Length;
                if (candidate >= distance) continue;
                distance = candidate;
                nearest = index;
            }
        }
        if (nearest is { } node)
        {
            NodeSelected?.Invoke(node);
            return;
        }
        foreach (var ring in Model.Rings.Take(Model.Rings.Count - 1))
        {
            double radius = ring.Radius * Model.Contraction * Scale * 1.13;
            if (Math.Abs((Screen(ring.Nodes[Model.SelectedNode]) - click).Length - radius) <= 8)
            {
                NodeSelected?.Invoke(Model.SelectedNode);
                return;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _drag = null;
    }
}
