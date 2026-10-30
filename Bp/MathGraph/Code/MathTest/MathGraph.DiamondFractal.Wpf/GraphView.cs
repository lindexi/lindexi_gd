using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MathGraph.DiamondFractal.Wpf.Model;

namespace MathGraph.DiamondFractal.Wpf;

internal sealed class GraphView : FrameworkElement
{
    private sealed record Dot(Point Center, double Radius, int Level);
    private sealed record Module(string Id, Point Center, double Radius, int Level);
    private readonly Dictionary<string, Dot> dots = [];
    private readonly List<Module> modules = [];
    private DiamondGraph graph = DiamondGraph.CreateDefault();
    private double zoom = 1;
    private Vector pan;
    private Point? dragStart;
    private bool dragged;
    internal event Action? Changed;
    internal event Action<string, bool>? Selected;
    internal event Action? LimitReached;
    internal DiamondGraph Graph => graph;
    internal string? SelectedModule { get; set; }

    /// <summary>Initializes the interactive drawing surface.</summary>
    public GraphView()
    {
        ClipToBounds = true;
        Focusable = true;
        SizeChanged += (_, _) => InvalidateVisual();
    }

    internal void SetGraph(DiamondGraph value)
    {
        graph = value;
        SelectedModule = null;
        InvalidateVisual();
        Changed?.Invoke();
    }

    internal void Refresh()
    {
        InvalidateVisual();
        Changed?.Invoke();
    }

    internal void Fit()
    {
        zoom = 1;
        pan = default;
        InvalidateVisual();
    }

    internal void Zoom(double factor)
    {
        zoom = Math.Clamp(zoom * factor, 0.25, 80);
        InvalidateVisual();
    }

    private double Scale => Math.Max(0.01, Math.Min(ActualWidth / 1000, ActualHeight / 700)) * zoom;
    private Point Screen(Point point) => new(ActualWidth / 2 + (point.X - 500) * Scale + pan.X,
        ActualHeight / 2 + (point.Y - 350) * Scale + pan.Y);
    private static Brush Color(int level) => level % 2 == 0 ? Brushes.DodgerBlue : Brushes.Gold;

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(Brushes.White, null, new Rect(RenderSize));
        dots.Clear();
        modules.Clear();
        Layout("root", new Point(525, 350), 550, 350, 0);
        dots["S"] = new(new Point(95, 350), 18, 0);
        dots["T"] = new(new Point(920, 350), 18, 0);
        foreach (var module in modules)
        {
            var center = Screen(module.Center);
            dc.DrawEllipse(Color(module.Level), new Pen(module.Id == SelectedModule ? Brushes.DarkOrange : Color(module.Level), 2),
                center, module.Radius * Scale, module.Radius * Scale);
        }
        foreach (var edge in graph.Edges)
        {
            var from = dots[edge.From];
            var to = dots[edge.To];
            var a = Screen(from.Center);
            var b = Screen(to.Center);
            Vector direction = b - a;
            if (direction.Length < 0.01) continue;
            direction.Normalize();
            a += direction * from.Radius * Scale;
            b -= direction * to.Radius * Scale;
            bool boundary = edge.From == "S" || edge.To == "T";
            var brush = boundary ? Brushes.MidnightBlue : Color(Math.Min(from.Level, to.Level));
            double width = Math.Clamp(2.4 * Scale, 1, 4);
            var pen = new Pen(brush, width);
            if (boundary) pen.DashStyle = DashStyles.Dash;
            dc.DrawLine(pen, a, b);
            foreach (var module in modules)
            {
                dc.PushClip(new EllipseGeometry(Screen(module.Center), module.Radius * Scale, module.Radius * Scale));
                dc.DrawLine(new Pen(Color(module.Level + 1), width), a, b);
                dc.Pop();
            }
            brush = boundary ? Brushes.MidnightBlue : Color(to.Level);
            double arrow = Math.Min(Math.Clamp(9 * Scale, 3, 13), (b - a).Length * 0.35);
            Vector normal = new(-direction.Y, direction.X);
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(b, true, true);
                context.LineTo(b - direction * arrow + normal * arrow * 0.45, true, false);
                context.LineTo(b - direction * arrow - normal * arrow * 0.45, true, false);
            }
            dc.DrawGeometry(brush, null, geometry);
        }
        foreach (var vertex in graph.Vertices)
        {
            var dot = dots[vertex.Id];
            var center = Screen(dot.Center);
            double radius = dot.Radius * Scale;
            dc.DrawEllipse(vertex.IsBoundary ? Brushes.MidnightBlue : Color(dot.Level), null, center, radius, radius);
            if (vertex.IsBoundary)
                DrawText(dc, (string)FindResource("External"), center + new Vector(-18, 25), 13, Brushes.MidnightBlue);
            else if (radius >= 6)
                DrawText(dc, vertex.Id.Split('/')[^1], center + new Vector(radius + 4, -9), 12, dot.Level % 2 == 0 ? Brushes.RoyalBlue : Brushes.Gold);
        }
    }

    private void Layout(string path, Point center, double width, double height, int level)
    {
        double radius = width * 0.028;
        dots[path + "/L"] = new(new Point(center.X - width / 2, center.Y), radius, level);
        dots[path + "/R"] = new(new Point(center.X + width / 2, center.Y), radius, level);
        foreach (string branch in new[] { "U", "D" })
        {
            var position = new Point(center.X, center.Y + (branch == "U" ? -1 : 1) * height / 2);
            string child = path + "/" + branch;
            if (graph.ExpandedPaths.Contains(child))
            {
                double containerRadius = Math.Min(width * 0.24, height * 0.42);
                modules.Add(new(child, position, containerRadius, level));
                // Child extrema remain strictly within the parent disk at every recursive level.
                Layout(child, position, containerRadius * 1.44, containerRadius * 1.15, level + 1);
            }
            else dots[child] = new(position, radius, level);
        }
    }

    private void DrawText(DrawingContext dc, string text, Point position, double size, Brush brush) =>
        dc.DrawText(new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface("Microsoft YaHei UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip), position);

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        dragStart = e.GetPosition(this);
        dragged = false;
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragStart is not Point previous || e.LeftButton != MouseButtonState.Pressed) return;
        var position = e.GetPosition(this);
        var delta = position - previous;
        if (!dragged && delta.Length < 4) return;
        dragged = true;
        pan += delta;
        dragStart = position;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();
        dragStart = null;
        if (dragged) return;
        var point = e.GetPosition(this);
        var hit = dots.Where(pair => (Screen(pair.Value.Center) - point).Length <= Math.Max(10, pair.Value.Radius * Scale))
            .OrderBy(pair => (Screen(pair.Value.Center) - point).Length).FirstOrDefault();
        if (hit.Key is not null)
        {
            var vertex = graph.Vertices.Single(v => v.Id == hit.Key);
            Selected?.Invoke(vertex.Id, false);
            if (vertex.CanExpand)
            {
                try { graph.Expand(vertex.Id); Refresh(); }
                catch (ArgumentOutOfRangeException) { LimitReached?.Invoke(); }
                catch (InvalidOperationException) { LimitReached?.Invoke(); }
            }
            return;
        }
        var module = modules.LastOrDefault(m => (Screen(m.Center) - point).Length <= m.Radius * Scale);
        if (module is null) return;
        SelectedModule = module.Id;
        Selected?.Invoke(module.Id, true);
        InvalidateVisual();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        var point = e.GetPosition(this);
        double old = Scale;
        zoom = Math.Clamp(zoom * (e.Delta > 0 ? 1.2 : 1 / 1.2), 0.25, 80);
        double ratio = Scale / old;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        pan = (point - center) - ((point - center) - pan) * ratio;
        InvalidateVisual();
        e.Handled = true;
    }
}
