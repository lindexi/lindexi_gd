using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation;

namespace MathGraph.PostExpansionLearning.Wpf;

public partial class MainWindow : Window
{
    private const int Updates = 24;
    private const double LearningRate = 1.0 / 16;

    private readonly (string Name, string Description, BranchParameters Initial, bool Masked)[] _strategies =
    [
        ("硬屏蔽", "(1, 1, 0)：有效门固定为零，训练不能解除屏蔽。", new(1, 1, 0), true),
        ("可训练零门", "(0, 1, 0)：门先更新，权重随后更新。", new(0, 1, 0), false),
        ("非零差分抵消", "(1, 1, 1)：初始支路抵消，独立权重首步向相反方向更新。", new(1, 1, 1), false),
        ("对称零门", "(0, 1, 1)：三个偏导在初始化时均为零。", new(0, 1, 1), false)
    ];

    /// <summary>Initializes the direction and metric controls and displays the first comparison.</summary>
    public MainWindow()
    {
        InitializeComponent();
        DirectionSelector.SelectionChanged += SelectionChanged;
        MetricSelector.SelectionChanged += SelectionChanged;
        RefreshCharts();
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshCharts();

    private void RefreshCharts()
    {
        bool split = DirectionSelector.SelectedIndex == 1;
        bool showGradient = MetricSelector.SelectedIndex == 1;
        ExperimentText.Text = split
            ? "单样本：t = 1，目标为 (2, −1)。同步梯度下降，步长 1/16。"
            : "单样本：(x, y) = (1, 0)，目标为 2。同步梯度下降，步长 1/16。";
        MetricText.Text = showGradient
            ? "纵轴：损失对参数的有符号偏导。四图共用刻度；重合的零梯度以灰线标示。"
            : "纵轴：半平方损失（各输出残差平方和的一半）。四图共用刻度。";
        GradientLegend.Visibility = showGradient ? Visibility.Visible : Visibility.Collapsed;

        var runs = _strategies.Select(strategy => Evaluate(split, strategy.Initial, strategy.Masked)).ToArray();
        double bound = runs.SelectMany(run => run).Max(sample => showGradient
            ? Math.Max(Math.Abs(sample.Gradient.Gate), Math.Max(Math.Abs(sample.Gradient.Positive), Math.Abs(sample.Gradient.Negative)))
            : sample.Loss);

        Charts.Children.Clear();
        for (int i = 0; i < _strategies.Length; i++)
        {
            var strategy = _strategies[i];
            var run = runs[i];
            var chart = new TrainingChart(run, showGradient, bound) { Height = 170 };
            var first = run[0].Gradient;
            var last = run[^1].Gradient;
            string summary = showGradient
                ? $"第 0 步 (g, p, n)：{Format(first)}；第 {Updates} 步：{Format(last)}"
                : $"第 0 步：{run[0].Loss:G5}；第 {Updates} 步：{run[^1].Loss:G5}";
            AutomationProperties.SetName(chart, $"{strategy.Name}，{summary}");
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = strategy.Name, FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBlock { Text = strategy.Description, TextWrapping = TextWrapping.Wrap, MinHeight = 38 });
            panel.Children.Add(chart);
            panel.Children.Add(new TextBlock { Text = summary, TextWrapping = TextWrapping.Wrap });
            Charts.Children.Add(new Border
            {
                Margin = new Thickness(6),
                Padding = new Thickness(10),
                BorderBrush = SystemColors.ActiveBorderBrush,
                BorderThickness = new Thickness(1),
                Child = panel
            });
        }
    }

    private static string Format(BranchParameters gradient) => $"({gradient.Gate:G5}, {gradient.Positive:G5}, {gradient.Negative:G5})";

    private static LossGradient[] Evaluate(bool split, BranchParameters parameters, bool masked)
    {
        var samples = new LossGradient[Updates + 1];
        for (int step = 0; step <= Updates; step++)
        {
            var sample = split
                ? FivePoint.SplitLoss(1, 2, -1, parameters, masked)
                : FivePoint.MergeLoss(1, 0, 2, parameters, masked);
            samples[step] = sample;
            if (step < Updates)
            {
                var gradient = sample.Gradient;
                parameters = new(
                    parameters.Gate - LearningRate * gradient.Gate,
                    parameters.Positive - LearningRate * gradient.Positive,
                    parameters.Negative - LearningRate * gradient.Negative);
            }
        }

        return samples;
    }
}
