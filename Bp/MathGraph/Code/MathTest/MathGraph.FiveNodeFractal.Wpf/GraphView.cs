using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MathGraph.FiveNodeFractal.Wpf.Model;

namespace MathGraph.FiveNodeFractal.Wpf;

internal sealed class GraphView : FrameworkElement
{
    private sealed record Dot(Point Center, bool Adapter, int Depth);
    private readonly Dictionary<string, Dot> dots = [];
    private readonly Dictionary<string, Rect> boxes = [];
    private readonly Dictionary<string, Size> sizes = [];
    private FiveNodeGraph graph = new();
    private double zoom = 1;
    private double scale = 1;
    private Vector pan;
    private Point? down;
    private bool moved;
    private Size world;
    internal event Action<string, bool>? Selected;
    internal event Action? Changed;
    internal event Action? Rejected;
    internal string? SelectedModule { get; set; }
    internal FiveNodeGraph Graph => graph;

    /// <summary>Creates a zoomable graph drawing surface.</summary>
    public GraphView()
    {
        ClipToBounds = true;
        SizeChanged += (_, _) => InvalidateVisual();
    }

    internal void SetGraph(FiveNodeGraph value)
    {
        graph = value;
        SelectedModule = null;
        Fit();
        Refresh();
    }
    internal void Refresh() { InvalidateVisual(); Changed?.Invoke(); }
    internal void Fit() { zoom = 1; pan = default; InvalidateVisual(); }
    internal void Zoom(double factor) => Zoom(factor, new Point(ActualWidth / 2, ActualHeight / 2));
    private void Zoom(double factor, Point anchor)
    {
        double nextZoom = zoom * factor;
        double nextScale = scale * factor;
        var nextPan = (anchor - new Point(ActualWidth / 2, ActualHeight / 2)) * (1 - factor) + pan * factor;
        if (!double.IsFinite(factor) || factor <= 0 || !double.IsFinite(nextZoom) || nextZoom <= 0 ||
            !IsSafeScale(nextScale, nextPan)) return;
        zoom = nextZoom;
        pan = nextPan;
        InvalidateVisual();
    }
    private bool IsSafeScale(double value, Vector offset) => double.IsFinite(value) && value > 0 &&
        double.IsFinite(world.Width * value + ActualWidth + Math.Abs(offset.X)) &&
        double.IsFinite(world.Height * value + ActualHeight + Math.Abs(offset.Y));
    private Point Screen(Point p) => new((p.X - world.Width / 2) * scale + ActualWidth / 2 + pan.X,
        (p.Y - world.Height / 2) * scale + ActualHeight / 2 + pan.Y);
    private static Brush Color(int depth) => depth % 2 == 0 ? Brushes.DodgerBlue : Brushes.Goldenrod;
    private string[] Children(string path) => graph.Vertices.Where(v => v.Id.StartsWith(path + "/", StringComparison.Ordinal))
        .Select(v => v.Id[(path.Length + 1)..].Split('/')[0]).Distinct().ToArray();
    private static string[][] Columns(string[] children) => new[] { new[] { "I" }, new[] { "A", "B" }, new[] { "C", "D" }, new[] { "E" }, new[] { "O" } }
        .Select(c => c.Where(children.Contains).ToArray()).Where(c => c.Length > 0).ToArray();

    private Size Measure(string path)
    {
        if (path != "root" && !graph.ExpandedPaths.Contains(path)) return sizes[path] = new(80, 70);
        var columns = Columns(Children(path));
        foreach (var name in columns.SelectMany(c => c)) Measure(path + "/" + name);
        return sizes[path] = new(columns.Sum(c => c.Max(n => sizes[path + "/" + n].Width)) + 70 * (columns.Length - 1) + 60,
            columns.Max(c => c.Sum(n => sizes[path + "/" + n].Height) + (c.Length - 1) * 60) + 70);
    }

    private void Place(string path, Rect rect)
    {
        if (path != "root" && !graph.ExpandedPaths.Contains(path))
        {
            var vertex = graph.Vertices.Single(v => v.Id == path);
            dots[path] = new(new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), vertex.IsAdapter, vertex.Depth);
            return;
        }
        if (path != "root") boxes[path] = rect;
        double x = rect.X + 30;
        foreach (var column in Columns(Children(path)))
        {
            double width = column.Max(n => sizes[path + "/" + n].Width);
            double height = column.Sum(n => sizes[path + "/" + n].Height) + (column.Length - 1) * 60;
            double y = rect.Y + (rect.Height - height) / 2;
            foreach (var name in column)
            {
                var size = sizes[path + "/" + name];
                Place(path + "/" + name, new Rect(x + (width - size.Width) / 2, y, size.Width, size.Height));
                y += size.Height + 60;
            }
            x += width + 70;
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(Brushes.White, null, new Rect(RenderSize));
        dots.Clear(); boxes.Clear(); sizes.Clear();
        var rootSize = Measure("root");
        world = new(rootSize.Width + 240, rootSize.Height + 80);
        double nextScale = Math.Min(ActualWidth / world.Width, ActualHeight / world.Height) * 0.92 * zoom;
        if (!IsSafeScale(nextScale, pan)) return;
        scale = nextScale;
        Place("root", new Rect(120, 40, rootSize.Width, rootSize.Height));
        dots["S0"] = new(new(35, dots[graph.GetBoundary("root").Inputs[0].VertexId].Center.Y), false, 0);
        dots["S1"] = new(new(35, dots[graph.GetBoundary("root").Inputs[1].VertexId].Center.Y), false, 0);
        dots["T"] = new(new(world.Width - 35, world.Height / 2), false, 0);
        foreach (var (path, box) in boxes.OrderBy(b => b.Key.Length))
        {
            var rect = new Rect(Screen(box.TopLeft), Screen(box.BottomRight));
            var brush = Color(path.Count(c => c == '/'));
            dc.DrawRoundedRectangle(new SolidColorBrush(System.Windows.Media.Color.FromArgb(12, 40, 100, 180)),
                new Pen(path == SelectedModule ? Brushes.OrangeRed : brush, 1.5), rect, 8, 8);
            if (rect.Width > 100) Text(dc, path.Split('/').Last(), rect.TopLeft + new Vector(5, 3), Brushes.SlateGray, 11);
        }
        foreach (var edge in graph.Edges)
        {
            var from = dots[edge.Source.VertexId]; var to = dots[edge.Target.VertexId];
            var a = Screen(from.Center); var b = Screen(to.Center);
            Vector direction = b - a;
            if (direction.Length < 0.01) continue;
            direction.Normalize();
            double radius = Math.Max(1, 17 * scale);
            a += direction * radius; b -= direction * radius;
            var brush = Color(Math.Min(from.Depth, to.Depth));
            dc.DrawLine(new Pen(brush, 1.4), a, b);
            Vector side = new(-direction.Y, direction.X);
            double arrow = Math.Min(8, (b - a).Length / 4);
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(b, true, true);
                context.LineTo(b - direction * arrow + side * arrow / 2, true, false);
                context.LineTo(b - direction * arrow - side * arrow / 2, true, false);
            }
            dc.DrawGeometry(brush, null, geometry);
            if (scale > 0.65)
            {
                Text(dc, edge.Source.Index.ToString(CultureInfo.InvariantCulture), a + direction * 12 + side * 6, brush, 10);
                Text(dc, edge.Target.Index.ToString(CultureInfo.InvariantCulture), b - direction * 24 + side * 6, brush, 10);
            }
        }
        foreach (var (id, dot) in dots)
        {
            var center = Screen(dot.Center);
            double radius = Math.Max(1.5, 17 * scale);
            var brush = Color(dot.Depth);
            if (dot.Adapter) dc.DrawRectangle(Brushes.White, new Pen(brush, 2), new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2));
            else dc.DrawEllipse(Brushes.White, new Pen(brush, 2), center, radius, radius);
            if (scale > 0.35)
            {
                string label = id.Split('/').Last();
                Text(dc, label, center - new Vector(label.Length * 4, 8), Brushes.MidnightBlue, 12);
                if (!graph.Vertices.Single(v => v.Id == id).IsBoundary && scale > 0.65)
                {
                    var degree = graph.GetDegree(id);
                    Text(dc, $"{degree.Incoming}{degree.Outgoing}", center + new Vector(-8, radius + 3), Brushes.SlateGray, 10);
                }
            }
        }
    }

    private void Text(DrawingContext dc, string text, Point origin, Brush brush, double size) => dc.DrawText(
        new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip), origin);

    protected override void OnMouseWheel(MouseWheelEventArgs e) { Zoom(e.Delta > 0 ? 1.2 : 1 / 1.2, e.GetPosition(this)); e.Handled = true; }
    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        var point = e.GetPosition(this);
        var leaf = dots.Where(d => !graph.Vertices.Single(v => v.Id == d.Key).IsBoundary)
            .Select(d => (d.Key, Distance: (Screen(d.Value.Center) - point).Length))
            .Where(d => d.Distance <= Math.Max(8, 18 * scale)).OrderBy(d => d.Distance).FirstOrDefault();
        string? path = leaf.Key is not null
            ? graph.ExpandedPaths.Where(p => leaf.Key.StartsWith(p + "/", StringComparison.Ordinal)).OrderByDescending(p => p.Length).FirstOrDefault()
            : boxes.Where(b => new Rect(Screen(b.Value.TopLeft), Screen(b.Value.BottomRight)).Contains(point))
                .OrderByDescending(b => b.Key.Length).Select(b => b.Key).FirstOrDefault();
        if (path is not null)
        {
            graph.Collapse(path);
            SelectedModule = null;
            Refresh();
            Selected?.Invoke(path, false);
        }
        e.Handled = true;
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { down = e.GetPosition(this); moved = false; CaptureMouse(); }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (down is not Point start) return;
        var now = e.GetPosition(this);
        if ((now - start).Length < 4 && !moved) return;
        pan += now - start; down = now; moved = true; InvalidateVisual();
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        ReleaseMouseCapture(); down = null;
        if (moved) return;
        var point = e.GetPosition(this);
        var hit = dots.Where(d => !graph.Vertices.Single(v => v.Id == d.Key).IsBoundary)
            .Select(d => (d.Key, Distance: (Screen(d.Value.Center) - point).Length))
            .Where(d => d.Distance <= Math.Max(8, 18 * scale)).OrderBy(d => d.Distance).FirstOrDefault();
        if (hit.Key is not null)
        {
            Selected?.Invoke(hit.Key, false);
            try { graph.Expand(hit.Key); Refresh(); }
            catch (InvalidOperationException) { Rejected?.Invoke(); }
            catch (ArgumentOutOfRangeException) { Rejected?.Invoke(); }
            return;
        }
        foreach (var box in boxes.OrderByDescending(b => b.Key.Length))
        {
            if (!new Rect(Screen(box.Value.TopLeft), Screen(box.Value.BottomRight)).Contains(point)) continue;
            SelectedModule = box.Key; Selected?.Invoke(box.Key, true); InvalidateVisual(); break;
        }
    }
}
