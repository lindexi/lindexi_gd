using System.Globalization;
using BigInteger = System.Numerics.BigInteger;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Function = MathGraph.FunctionMorphism.PortFunction<System.Numerics.BigInteger>;

namespace MathGraph.FunctionMorphism.Wpf;

public partial class MainWindow : Window
{
    private const double NodeWidth = 86;
    private readonly Function forward = Function.Merge((x, y) => x * x - y);
    private readonly Function reverse = Function.Split(t => (t * t + 1, t * t * t - t));
    private readonly Function expandedForward;
    private readonly Function expandedReverse;

    /// <summary>Initializes the template selector and evaluates the initial example.</summary>
    public MainWindow()
    {
        expandedForward = forward.Expand();
        expandedReverse = reverse.Expand();
        InitializeComponent();
        TemplateSelector.SelectedIndex = 0;
        TemplateSelector.SelectionChanged += (_, _) => Refresh();
        FirstInput.TextChanged += (_, _) => Refresh();
        SecondInput.TextChanged += (_, _) => Refresh();
        Refresh();
    }

    private void Refresh()
    {
        bool isForward = TemplateSelector.SelectedIndex == 0;
        SecondInputGroup.Visibility = isForward ? Visibility.Visible : Visibility.Collapsed;
        Formula.Text = (string)FindResource(isForward ? "ForwardFormula" : "ReverseFormula");

        BigInteger second = default;
        if (!TryRead(FirstInput.Text, out var first) || (isForward && !TryRead(SecondInput.Text, out second)))
        {
            Status.Text = (string)FindResource("InvalidInput");
            Results.Visibility = Visibility.Collapsed;
            return;
        }

        BigInteger[] inputs = isForward ? [first, second] : [first];
        var before = (isForward ? forward : reverse).Evaluate(inputs);
        var trace = (isForward ? expandedForward : expandedReverse).EvaluateWithTrace(inputs);
        ComparisonGrid.ItemsSource = trace.Outputs.Select((value, port) => new
        {
            Port = port,
            Before = Format(before[port]),
            After = Format(value),
            Error = Format(BigInteger.Abs(value - before[port]))
        }).ToArray();
        WireGrid.ItemsSource = trace.Wires.Select(wire => new
        {
            Source = $"{wire.From}.{wire.OutputPort}",
            Target = $"{wire.To}.{wire.InputPort}",
            Value = Format(wire.Value)
        }).ToArray();
        string inputDirection = (string)FindResource("InputDirection");
        string outputDirection = (string)FindResource("OutputDirection");
        PortGrid.ItemsSource = trace.Children.SelectMany(child =>
            child.Inputs.Select((value, port) => new { child.Vertex, Direction = inputDirection, Port = port, Value = Format(value) })
                .Concat(child.Outputs.Select((value, port) => new { child.Vertex, Direction = outputDirection, Port = port, Value = Format(value) })))
            .ToArray();
        DrawGraph(trace);
        Status.Text = string.Empty;
        Results.Visibility = Visibility.Visible;
    }

    private static bool TryRead(string text, out BigInteger value) =>
        BigInteger.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static string Format(BigInteger value) => value.ToString(CultureInfo.InvariantCulture);

    private void DrawGraph(Function.EvaluationTrace trace)
    {
        Graph.Children.Clear();
        var levels = new Dictionary<char, int>();
        foreach (var child in trace.Children)
        {
            levels[child.Vertex] = trace.Wires.Where(wire => wire.To == child.Vertex)
                .Select(wire => levels[wire.From] + 1).DefaultIfEmpty(0).Max();
        }

        var positions = new Dictionary<char, Point>();
        foreach (var level in trace.Children.GroupBy(child => levels[child.Vertex]))
        {
            double x = 20 + 250 * level.Key;
            double y = level.Count() == 1 ? 54 : 12;
            foreach (var child in level)
            {
                positions.Add(child.Vertex, new Point(x, y));
                y += 84;
            }
        }

        foreach (var wire in trace.Wires)
        {
            var from = PortPosition(positions[wire.From], wire.OutputPort, output: true);
            var to = PortPosition(positions[wire.To], wire.InputPort, output: false);
            var line = new Line
            {
                X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y,
                Stroke = Brushes.SlateGray, StrokeThickness = 1.5,
                ToolTip = $"{wire.From}.{wire.OutputPort} → {wire.To}.{wire.InputPort}: {Format(wire.Value)}"
            };
            Graph.Children.Add(line);
            Vector direction = to - from;
            direction.Normalize();
            var side = new Vector(-direction.Y, direction.X);
            var tip = to - 4 * direction;
            Graph.Children.Add(new Polygon
            {
                Fill = Brushes.SlateGray,
                IsHitTestVisible = false,
                Points = new PointCollection { tip, tip - 8 * direction + 4 * side, tip - 8 * direction - 4 * side }
            });
        }

        foreach (var child in trace.Children)
        {
            var position = positions[child.Vertex];
            Place(new Border
            {
                Width = NodeWidth, Height = 56,
                Background = Brushes.WhiteSmoke, BorderBrush = Brushes.LightSlateGray,
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = child.Vertex.ToString(), FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 3, 0, 0)
                }
            }, position);
            DrawPorts(position, child.Inputs, output: false);
            DrawPorts(position, child.Outputs, output: true);
        }
    }

    private void DrawPorts(Point node, IReadOnlyList<BigInteger> values, bool output)
    {
        for (int port = 0; port < values.Count; port++)
        {
            var point = PortPosition(node, port, output);
            Place(new Ellipse
            {
                Width = 8, Height = 8, Fill = output ? Brushes.SeaGreen : Brushes.SteelBlue,
                ToolTip = Format(values[port])
            }, new Point(point.X - 4, point.Y - 4));
            Place(new TextBlock { Text = port.ToString(CultureInfo.InvariantCulture), FontSize = 11 },
                new Point(point.X + (output ? -14 : 7), point.Y - 8));
        }
    }

    private static Point PortPosition(Point node, int port, bool output) =>
        new(node.X + (output ? NodeWidth : 0), node.Y + 26 + 17 * port);

    private void Place(UIElement element, Point point)
    {
        Canvas.SetLeft(element, point.X);
        Canvas.SetTop(element, point.Y);
        Graph.Children.Add(element);
    }
}
