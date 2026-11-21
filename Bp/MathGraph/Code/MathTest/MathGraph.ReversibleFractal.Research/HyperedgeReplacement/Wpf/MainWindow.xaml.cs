using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MathGraph.HyperedgeReplacement.Wpf;

public partial class MainWindow : Window
{
    private const double VertexY = 112;
    private const double EdgeY = 32;

    private readonly Hypergraph _initial;
    private readonly Hyperedge _left;
    private readonly Hyperedge _right;
    private readonly Vertex _joint = new();
    private readonly ReplacementRule _orderedRule;
    private readonly ReplacementRule _reversedRule;
    private readonly Dictionary<Vertex, (string Name, double X)> _positions;
    private Hypergraph _current;

    /// <summary>Creates the shared-boundary example shown when the window opens.</summary>
    public MainWindow()
    {
        var start = new Vertex();
        var end = new Vertex();
        var nonterminal = new EdgeLabel("A", 2, false);
        _left = new Hyperedge(nonterminal, [start, _joint]);
        _right = new Hyperedge(nonterminal, [_joint, end]);
        _initial = new Hypergraph([start, _joint, end], [_left, _right]);
        _current = _initial;
        _positions = new()
        {
            [start] = ("s", 24),
            [_joint] = ("j", 220),
            [end] = ("t", 416),
        };

        var x = new Vertex();
        var y = new Vertex();
        var middle = new Vertex();
        var terminal = new EdgeLabel("link", 2, true);
        var template = new Hypergraph([x, middle, y],
            [new Hyperedge(terminal, [x, middle]), new Hyperedge(terminal, [middle, y])]);
        _orderedRule = new ReplacementRule(nonterminal, template, [x, y]);
        _reversedRule = new ReplacementRule(nonterminal, template, [y, x]);

        InitializeComponent();
        Refresh(_initial);
    }

    private void ExpandLeft_Click(object sender, RoutedEventArgs e) => Expand(_left, "u");

    private void ExpandRight_Click(object sender, RoutedEventArgs e) => Expand(_right, "v");

    private void Expand(Hyperedge selected, string newVertexName)
    {
        var before = _current;
        var rule = ReverseOrder.IsChecked == true ? _reversedRule : _orderedRule;
        _current = rule.Apply(before, selected);
        var newVertex = _current.Vertices.Except(before.Vertices).Single();
        _positions.Add(newVertex, (newVertexName, selected.Attachments.Average(vertex => _positions[vertex].X)));
        Refresh(before);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        foreach (var vertex in _current.Vertices.Except(_initial.Vertices))
        {
            _positions.Remove(vertex);
        }

        _current = _initial;
        ReverseOrder.IsChecked = false;
        Refresh(_initial);
    }

    private void Refresh(Hypergraph before)
    {
        DrawGraph(BeforeCanvas, before);
        DrawGraph(CurrentCanvas, _current);
        ExpandLeftButton.IsEnabled = _current.Edges.Contains(_left);
        ExpandRightButton.IsEnabled = _current.Edges.Contains(_right);
    }

    private void DrawGraph(Canvas canvas, Hypergraph graph)
    {
        canvas.Children.Clear();

        foreach (var edge in graph.Edges)
        {
            var edgeX = edge.Attachments.Average(vertex => _positions[vertex].X);
            for (var index = 0; index < edge.Attachments.Count; index++)
            {
                var vertexX = _positions[edge.Attachments[index]].X;
                canvas.Children.Add(new Line
                {
                    X1 = edgeX,
                    Y1 = EdgeY,
                    X2 = vertexX,
                    Y2 = VertexY,
                    Stroke = Brushes.SlateGray,
                    StrokeThickness = 2,
                });
                AddBadge(canvas, (edgeX + vertexX) / 2, (EdgeY + VertexY) / 2,
                    18, 18, (index + 1).ToString(), Brushes.White, Brushes.SlateGray, Brushes.Black, 9);
            }

            AddBadge(canvas, edgeX, EdgeY, 44, 28, edge.Label.Name,
                edge.Label.IsTerminal ? Brushes.DarkGreen : Brushes.DarkOrange,
                Brushes.Transparent, edge.Label.IsTerminal ? Brushes.White : Brushes.Black, 3);
        }

        foreach (var vertex in graph.Vertices)
        {
            var (name, x) = _positions[vertex];
            AddBadge(canvas, x, VertexY, 28, 28, name,
                vertex == _joint ? Brushes.DarkViolet : Brushes.MidnightBlue,
                Brushes.Transparent, Brushes.White, 14);
        }
    }

    private static void AddBadge(Canvas canvas, double x, double y, double width, double height,
        string text, Brush background, Brush borderBrush, Brush foreground, double radius)
    {
        var badge = new Border
        {
            Width = width,
            Height = height,
            Background = background,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(radius),
            Child = new TextBlock
            {
                Text = text,
                Foreground = foreground,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        Canvas.SetLeft(badge, x - width / 2);
        Canvas.SetTop(badge, y - height / 2);
        canvas.Children.Add(badge);
    }
}
