using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MathGraph.GdIfs.Wpf.Model;

namespace MathGraph.GdIfs.Wpf;

/// <summary>The research controls and model presentation.</summary>
public partial class MainWindow : Window
{
    private bool _ready;

    /// <summary>Initializes the interactive research window.</summary>
    public MainWindow()
    {
        InitializeComponent();
        for (int index = 1; index <= 5; index++)
            Selection.Items.Add(string.Format(CultureInfo.CurrentCulture, (string)FindResource("NodeFormat"), index));
        Graph.NodeSelected += index => Selection.SelectedIndex = index;
        _ready = true;
        Selection.SelectedIndex = 0;
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshModel();
    private void ParameterChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshModel();

    private void RefreshModel()
    {
        if (!_ready || Selection.SelectedIndex < 0) return;
        var model = CycleModel.Create(Selection.SelectedIndex, (int)Depth.Value, Ratio.Value);
        Graph.Model = model;
        DepthValue.Text = Depth.Value.ToString("0", CultureInfo.CurrentCulture);
        RatioValue.Text = Ratio.Value.ToString("F2", CultureInfo.CurrentCulture);
        Statistics.Text = string.Format(CultureInfo.CurrentCulture, ((string)FindResource("StatsFormat")).Replace("\\n", "\n"),
            model.Rings.Count, model.NodeCount, model.EdgeCount, model.Rings.Count - 1, model.Contraction);
    }

    private void ZoomInClick(object sender, RoutedEventArgs e) => Graph.Zoom(1.5);
    private void ZoomOutClick(object sender, RoutedEventArgs e) => Graph.Zoom(1 / 1.5);
    private void ResetClick(object sender, RoutedEventArgs e) => Graph.ResetView();
}
