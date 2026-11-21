using System.Globalization;
using System.Windows;
using MathGraph.ReversibleFractal.Research.NodeOperators;

namespace MathGraph.NodeOperators.Wpf;

public partial class MainWindow : Window
{
    private sealed record TopologyChoice(string Label, string[] ExpansionPaths);
    private sealed record ActivationChoice(string Label, Func<string, Activation> SelectActivation);
    private sealed record BarRow(string Label, double Value, double NegativeWidth, double PositiveWidth);

    public MainWindow()
    {
        InitializeComponent();

        TopologyBox.ItemsSource = new TopologyChoice[]
        {
            new(Text("BaseTopology"), []),
            new(Text("ForwardTopology"), ["C"]),
            new(Text("ReverseTopology"), ["A"]),
            new(Text("MixedTopology"), ["A", "C"]),
        };
        ActivationBox.ItemsSource = new ActivationChoice[]
        {
            new("Identity", static _ => Activation.Identity),
            new("Tanh", static _ => Activation.Tanh),
            new(Text("MixedActivation"), static path => path[^1] is 'A' or 'C' or 'E' ? Activation.Tanh : Activation.Identity),
        };
        TopologyBox.SelectedIndex = 0;
        ActivationBox.SelectedIndex = 0;
        FirstInput.Text = Format("{0:G}", 0.5);
        SecondInput.Text = Format("{0:G}", -1.0);

        TopologyBox.SelectionChanged += (_, _) => Recalculate();
        ActivationBox.SelectionChanged += (_, _) => Recalculate();
        FirstInput.TextChanged += (_, _) => Recalculate();
        SecondInput.TextChanged += (_, _) => Recalculate();
        Recalculate();
    }

    private void Recalculate()
    {
        if (TopologyBox.SelectedItem is not TopologyChoice topology ||
            ActivationBox.SelectedItem is not ActivationChoice activation)
        {
            ResultsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        if (!TryParseInput(FirstInput.Text, out double first) || !TryParseInput(SecondInput.Text, out double second))
        {
            StatusText.Text = Format(Text("InvalidInput"), CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            ResultsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var graph = FullCross.Create(topology.ExpansionPaths, activation.SelectActivation);
        var parameters = new double[graph.ParameterCount];
        for (int i = 0; i < parameters.Length; i++)
            parameters[i] = ((7 * i + 3) % 17 - 8) / 10.0;

        var result = graph.Differentiate([first, second], parameters, [1.0]);
        if (!AllFinite(result.Outputs) || !AllFinite(result.Inputs) || !AllFinite(result.Parameters))
        {
            StatusText.Text = Text("NonFiniteResult");
            ResultsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        SetChart(OutputGroup, OutputRows, Text("OutputTitle"), "y[{0}]", result.Outputs);
        SetChart(InputGroup, InputRows, Text("InputTitle"), "∂L/∂x[{0}]", result.Inputs);
        SetChart(ParameterGroup, ParameterRows, Text("ParameterTitle"), "∂L/∂p[{0}]", result.Parameters);
        StatusText.Text = Format(Text("GraphSummary"), graph.NodeCount, graph.ParameterCount);
        ResultsPanel.Visibility = Visibility.Visible;
    }

    private string Text(string key) => (string)FindResource(key);

    private static string Format(string format, params object[] values) => string.Format(CultureInfo.CurrentCulture, format, values);

    private static bool TryParseInput(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) && double.IsFinite(value);

    private static bool AllFinite(double[] values) => Array.TrueForAll(values, double.IsFinite);

    private void SetChart(System.Windows.Controls.GroupBox group, System.Windows.Controls.ItemsControl chart,
        string title, string labelFormat, double[] values)
    {
        double scale = values.Max(static value => Math.Abs(value));
        group.Header = Format(Text("ChartTitle"), title, scale);
        chart.ItemsSource = values.Select((value, i) =>
        {
            double width = scale == 0 ? 0 : Math.Abs(value) / scale * 100;
            return new BarRow(Format(labelFormat, i), value, value < 0 ? width : 0, value > 0 ? width : 0);
        }).ToArray();
    }
}
