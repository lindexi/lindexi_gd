using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using EdgePath = System.Windows.Shapes.Path;

namespace OrderedNodeReplacement.Wpf;

internal static class GraphDiagram
{
    private const double NodeWidth = 112;
    private const double NodeHeight = 74;
    private const double PortRadius = 4;
    private static readonly Brush Ink = Brushes.MidnightBlue;
    private static readonly Brush Wire = Brushes.SteelBlue;

    internal static void Draw(Canvas canvas, OrderedGraph graph, IReadOnlyDictionary<NodeId, Point> layout,
        Func<NodeId, bool> isHighlighted)
    {
        canvas.Children.Clear();
        foreach (var edge in graph.Edges)
        {
            DrawEdge(canvas, edge, layout);
        }

        foreach (var node in graph.Nodes)
        {
            DrawNode(canvas, node, layout[node.Id], isHighlighted(node.Id));
        }
    }

    private static void DrawEdge(Canvas canvas, Edge edge, IReadOnlyDictionary<NodeId, Point> layout)
    {
        var start = PortPosition(layout[edge.Source.Node], edge.Source.Index, output: true);
        var end = PortPosition(layout[edge.Target.Node], edge.Target.Index, output: false);
        start.X += PortRadius;
        end.X -= PortRadius;
        var bend = (end.X - start.X) * 0.45;
        var curve = new PathFigure { StartPoint = start };
        curve.Segments.Add(new BezierSegment(new(start.X + bend, start.Y), new(end.X - bend, end.Y), end, true));
        var geometry = new PathGeometry([curve]);
        var description = $"{DisplayName(edge.Source.Node)}:out{edge.Source.Index} → {DisplayName(edge.Target.Node)}:in{edge.Target.Index}";

        canvas.Children.Add(new EdgePath { Data = geometry, Stroke = Wire, StrokeThickness = 2.5 });
        canvas.Children.Add(new EdgePath { Data = geometry, Stroke = Brushes.Transparent, StrokeThickness = 12, ToolTip = description });
        canvas.Children.Add(new Polygon
        {
            Points = [end, new(end.X - 10, end.Y - 4), new(end.X - 10, end.Y + 4)],
            Fill = Wire,
            ToolTip = description
        });
    }

    private static void DrawNode(Canvas canvas, Node node, Point position, bool highlighted)
    {
        Place(canvas, new Border
        {
            Width = NodeWidth,
            Height = NodeHeight,
            Background = highlighted ? Brushes.LightBlue : Brushes.AliceBlue,
            BorderBrush = Wire,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = new TextBlock
            {
                Text = DisplayName(node.Id),
                Foreground = Ink,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        }, position);

        DrawPorts(node.InputCount, output: false);
        DrawPorts(node.OutputCount, output: true);

        void DrawPorts(int count, bool output)
        {
            for (var index = 0; index < count; index++)
            {
                var point = PortPosition(position, index, output);
                var label = $"{(output ? "out" : "in")}{index}";
                Place(canvas, new Ellipse
                {
                    Width = 2 * PortRadius,
                    Height = 2 * PortRadius,
                    Fill = Ink,
                    ToolTip = $"{DisplayName(node.Id)}:{label}"
                }, new(point.X - PortRadius, point.Y - PortRadius));
                Place(canvas, new TextBlock
                {
                    Width = 36,
                    Text = label,
                    TextAlignment = output ? TextAlignment.Right : TextAlignment.Left,
                    Foreground = Ink,
                    FontSize = 11
                }, new(point.X + (output ? -43 : 7), point.Y - 16));
            }
        }
    }

    private static Point PortPosition(Point node, int index, bool output) =>
        new(node.X + (output ? NodeWidth : 0), node.Y + 24 + index * 32);

    private static string DisplayName(NodeId id) => id.Parent is { } parent
        ? $"{DisplayName(parent)}/{id.Name}"
        : id.Name;

    private static void Place(Canvas canvas, UIElement element, Point position)
    {
        Canvas.SetLeft(element, position.X);
        Canvas.SetTop(element, position.Y);
        canvas.Children.Add(element);
    }
}
