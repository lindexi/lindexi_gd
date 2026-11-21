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
    private const double ValueFontSize = 12;
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
        string comparison = (string)FindResource("OutputComparison");
        OutputSummary.Text = string.Join(Environment.NewLine, trace.Outputs.Select((value, port) =>
            string.Format(CultureInfo.CurrentCulture, comparison, port, Format(before[port]), Format(value),
                Format(BigInteger.Abs(value - before[port])))));
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

        double valueWidth = trace.Children.SelectMany(child => child.Inputs.Concat(child.Outputs))
            .Max(value => MeasureLabel(new TextBlock { Text = $"0: {Format(value)}", FontSize = ValueFontSize }).Width);
        double nodeWidth = Math.Max(116, 2 * valueWidth + 26);
        double columnStep = nodeWidth + Math.Max(140, 3 * valueWidth + 32);
        Graph.Width = columnStep * levels.Values.Max() + nodeWidth + 40;
        var wireLabels = new List<(TextBlock Label, Point Anchor)>();
        var positions = new Dictionary<char, Point>();
        foreach (var level in trace.Children.GroupBy(child => levels[child.Vertex]))
        {
            double x = 20 + columnStep * level.Key;
            double y = level.Count() == 1 ? 54 : 12;
            foreach (var child in level)
            {
                positions.Add(child.Vertex, new Point(x, y));
                y += 84;
            }
        }

        foreach (var wire in trace.Wires)
        {
            var from = PortPosition(positions[wire.From], nodeWidth, wire.OutputPort, output: true);
            var to = PortPosition(positions[wire.To], nodeWidth, wire.InputPort, output: false);
            string value = Format(wire.Value);
            string description = $"{wire.From}.{wire.OutputPort} → {wire.To}.{wire.InputPort}: {value}";
            var line = new Line
            {
                X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y,
                Stroke = Brushes.SlateGray, StrokeThickness = 1.5,
                ToolTip = description
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
            // Keep labels on crossing wires away from their common midpoint.
            double fraction = to.Y < from.Y ? 0.68 : 0.32;
            wireLabels.Add((new TextBlock
            {
                Text = value, FontSize = ValueFontSize, Foreground = Brushes.DarkSlateGray,
                Background = Brushes.White, Padding = new Thickness(3, 0, 3, 0), ToolTip = description
            }, from + fraction * (to - from)));
        }

        foreach (var (label, anchor) in wireLabels)
        {
            var size = MeasureLabel(label);
            Place(label, new Point(anchor.X - size.Width / 2, anchor.Y - size.Height / 2));
        }

        foreach (var child in trace.Children)
        {
            var position = positions[child.Vertex];
            Place(new Border
            {
                Width = nodeWidth, Height = 58,
                Background = Brushes.WhiteSmoke, BorderBrush = Brushes.LightSlateGray,
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = child.Vertex.ToString(), FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 3, 0, 0)
                }
            }, position);
            DrawPorts(position, nodeWidth, child.Inputs, output: false);
            DrawPorts(position, nodeWidth, child.Outputs, output: true);
        }
    }

    private void DrawPorts(Point node, double nodeWidth, IReadOnlyList<BigInteger> values, bool output)
    {
        for (int port = 0; port < values.Count; port++)
        {
            var point = PortPosition(node, nodeWidth, port, output);
            var color = output ? Brushes.SeaGreen : Brushes.SteelBlue;
            Place(new Ellipse
            {
                Width = 8, Height = 8, Fill = color,
                ToolTip = Format(values[port])
            }, new Point(point.X - 4, point.Y - 4));
            var label = new TextBlock { Text = $"{port}: {Format(values[port])}", FontSize = ValueFontSize, Foreground = color };
            var size = MeasureLabel(label);
            Place(label, new Point(point.X + (output ? -size.Width - 7 : 7), point.Y - size.Height / 2));
        }
    }

    private static Point PortPosition(Point node, double nodeWidth, int port, bool output) =>
        new(node.X + (output ? nodeWidth : 0), node.Y + 26 + 17 * port);

    private static Size MeasureLabel(TextBlock label)
    {
        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return label.DesiredSize;
    }

    private void Place(UIElement element, Point point)
    {
        Canvas.SetLeft(element, point.X);
        Canvas.SetTop(element, point.Y);
        Graph.Children.Add(element);
    }
}
