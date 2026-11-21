using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MathGraph.ReversibleFractal.Wpf.Model;

namespace MathGraph.ReversibleFractal.Wpf;

internal sealed class GraphView : FrameworkElement
{
    private readonly Dictionary<string, Point> dots = [];
    private readonly Dictionary<string, Rect> boxes = [];
    private readonly Dictionary<string, Size> sizes = [];
    private HashSet<string> modules = [];
    private ReversibleGraph graph = new();
    private double zoom = 1, scale = 1;
    private Vector pan;
    private Point? down;
    private bool moved;
    private Size world;
    internal event Action? Changed;
    internal event Action<string>? Selected;
    internal string? SelectedModule { get; set; }
    internal ReversibleGraph Graph => graph;

    public GraphView()
    {
        ClipToBounds = true;
        SizeChanged += (_, _) => InvalidateVisual();
    }
    internal void SetGraph(ReversibleGraph value) { graph = value; SelectedModule = null; Fit(); Refresh(); }
    internal void Refresh() { InvalidateVisual(); Changed?.Invoke(); }
    internal void Fit() { zoom = 1; pan = default; InvalidateVisual(); }
    internal void Zoom(double factor) => Zoom(factor, new Point(ActualWidth / 2, ActualHeight / 2));
    private bool Safe(double value, Vector offset) => double.IsFinite(value) && value > 0 &&
        double.IsFinite(offset.X) && double.IsFinite(offset.Y) &&
        double.IsFinite(world.Width * value + ActualWidth + Math.Abs(offset.X)) &&
        double.IsFinite(world.Height * value + ActualHeight + Math.Abs(offset.Y));
    private void Zoom(double factor, Point anchor)
    {
        double next = zoom * factor;
        var offset = (anchor - new Point(ActualWidth / 2, ActualHeight / 2)) * (1 - factor) + pan * factor;
        if (!double.IsFinite(factor) || factor <= 0 || !double.IsFinite(next) || next <= 0 || !Safe(scale * factor, offset)) return;
        zoom = next; pan = offset; InvalidateVisual();
    }
    private Point Screen(Point p) => new((p.X - world.Width / 2) * scale + ActualWidth / 2 + pan.X,
        (p.Y - world.Height / 2) * scale + ActualHeight / 2 + pan.Y);
    private static Brush Color(string path) => path.Count(c => c == '/') % 2 == 0 ? Brushes.Goldenrod : Brushes.DodgerBlue;
    private string[][] Columns(string path) => graph.GetDirection(path) == Direction.Forward
        ? [["A", "B"], ["C", "D"], ["E"]] : [["E"], ["C", "D"], ["A", "B"]];
    private Size Measure(string path)
    {
        if (path != "root" && !modules.Contains(path)) return sizes[path] = new(80, 70);
        var columns = Columns(path);
        foreach (string name in columns.SelectMany(c => c)) Measure(path + "/" + name);
        double width = columns.Sum(c => c.Max(n => sizes[path + "/" + n].Width)) + 200;
        double height = columns.Max(c => c.Sum(n => sizes[path + "/" + n].Height) + (c.Length - 1) * 60) + 70;
        return sizes[path] = new(width, height);
    }
    private void Place(string path, Rect rect)
    {
        if (path != "root" && !modules.Contains(path)) { dots[path] = new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2); return; }
        if (path != "root") boxes[path] = rect;
        double x = rect.X + 30;
        foreach (var column in Columns(path))
        {
            double width = column.Max(n => sizes[path + "/" + n].Width);
            double height = column.Sum(n => sizes[path + "/" + n].Height) + (column.Length - 1) * 60;
            double y = rect.Y + (rect.Height - height) / 2;
            foreach (string name in column)
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
        dots.Clear(); boxes.Clear(); sizes.Clear(); modules = graph.ExpandedPaths.ToHashSet(StringComparer.Ordinal);
        var size = Measure("root"); world = new(size.Width + 240, size.Height + 80);
        double next = Math.Min(ActualWidth / world.Width, ActualHeight / world.Height) * 0.92 * zoom;
        if (!Safe(next, pan)) return;
        scale = next;
        Place("root", new Rect(120, 40, size.Width, size.Height));
        dots["S0"] = new(35, dots[graph.GetBoundary("root").Inputs[0].VertexId].Y);
        dots["S1"] = new(35, dots[graph.GetBoundary("root").Inputs[1].VertexId].Y);
        dots["T"] = new(world.Width - 35, world.Height / 2);
        foreach (var (path, box) in boxes.OrderBy(b => b.Key.Length))
        {
            var rect = new Rect(Screen(box.TopLeft), Screen(box.BottomRight));
            dc.DrawRoundedRectangle(new SolidColorBrush(System.Windows.Media.Color.FromArgb(12, 40, 100, 180)),
                new Pen(path == SelectedModule ? Brushes.OrangeRed : Color(path), 1.5), rect, 8, 8);
            if (rect.Width > 100) Text(dc, path.Split('/').Last() + (graph.GetDirection(path) == Direction.Forward ? " +" : " −"), rect.TopLeft + new Vector(5, 3), Brushes.SlateGray, 11);
        }
        foreach (var edge in graph.Edges)
        {
            var a = Screen(dots[edge.Source.VertexId]); var b = Screen(dots[edge.Target.VertexId]);
            Vector direction = b - a;
            if (direction.Length < 0.01 || !double.IsFinite(direction.Length)) continue;
            direction.Normalize();
            double radius = Math.Max(1, 17 * scale);
            a += direction * radius; b -= direction * radius;
            var brush = Color(edge.Source.VertexId);
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
        foreach (var vertex in graph.Vertices)
        {
            var center = Screen(dots[vertex.Id]); double radius = Math.Max(1.5, 17 * scale);
            dc.DrawEllipse(Brushes.White, new Pen(Color(vertex.Id), 2), center, radius, radius);
            if (scale > 0.35) Text(dc, vertex.Id.Split('/').Last(), center - new Vector(5, 8), Brushes.MidnightBlue, 12);
            if (!vertex.IsBoundary && scale > 0.65)
            {
                var degree = graph.GetSignature(vertex.Id);
                Text(dc, $"{degree.Incoming}{degree.Outgoing}", center + new Vector(-8, radius + 3), Brushes.SlateGray, 10);
            }
        }
    }
    private void Text(DrawingContext dc, string text, Point origin, Brush brush, double size) => dc.DrawText(
        new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip), origin);
    private string? HitLeaf(Point point) => dots.Where(d => d.Key.StartsWith("root/", StringComparison.Ordinal))
        .Select(d => (d.Key, Distance: (Screen(d.Value) - point).Length)).Where(d => d.Distance <= Math.Max(8, 18 * scale))
        .OrderBy(d => d.Distance).Select(d => d.Key).FirstOrDefault();
    private string? HitModule(Point point) => boxes.Where(b => new Rect(Screen(b.Value.TopLeft), Screen(b.Value.BottomRight)).Contains(point))
        .OrderByDescending(b => b.Key.Length).Select(b => b.Key).FirstOrDefault();
    protected override void OnMouseWheel(MouseWheelEventArgs e) { Zoom(e.Delta > 0 ? 1.2 : 1 / 1.2, e.GetPosition(this)); e.Handled = true; }
    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        var point = e.GetPosition(this); string? leaf = HitLeaf(point);
        string? path = leaf is null ? HitModule(point) : modules.Where(p => leaf.StartsWith(p + "/", StringComparison.Ordinal)).OrderByDescending(p => p.Length).FirstOrDefault();
        if (path is not null) { graph.Collapse(path); SelectedModule = null; Refresh(); Selected?.Invoke(path); }
        e.Handled = true;
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { down = e.GetPosition(this); moved = false; CaptureMouse(); }
    protected override void OnLostMouseCapture(MouseEventArgs e) { down = null; base.OnLostMouseCapture(e); }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (down is not Point start) return;
        var now = e.GetPosition(this);
        if ((now - start).Length < 4 && !moved) return;
        var offset = pan + (now - start);
        if (Safe(scale, offset)) pan = offset;
        down = now; moved = true; InvalidateVisual();
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        bool clicked = down is not null && !moved;
        ReleaseMouseCapture(); down = null;
        if (!clicked) return;
        var point = e.GetPosition(this); string? leaf = HitLeaf(point);
        if (leaf is not null) { graph.Expand(leaf); SelectedModule = leaf; Refresh(); Selected?.Invoke(leaf); return; }
        SelectedModule = HitModule(point);
        if (SelectedModule is not null) Selected?.Invoke(SelectedModule);
        InvalidateVisual();
    }
}
