using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MathGraph.DiamondFractal.Wpf.Model;

namespace MathGraph.DiamondFractal.Wpf;

/// <summary>Interactive finite diamond graph experiment.</summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes the reference-image graph and controls.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Graph.Changed += Update;
        Graph.Selected += Select;
        Graph.LimitReached += () => Status.Text = Text("Limit");
        Update();
    }

    private string Text(string key) => (string)FindResource(key);
    private void Update()
    {
        var graph = Graph.Graph;
        Statistics.Text = string.Format(CultureInfo.CurrentCulture, Text("Statistics"), graph.RealVertexCount,
            graph.Vertices.Count, graph.Edges.Count, graph.ExpandedPaths.Count, graph.Depth);
        string? selection = Graph.SelectedModule;
        Modules.ItemsSource = graph.ExpandedPaths;
        Modules.SelectedItem = selection;
        SelectionInfo.Text = "";
        Status.Text = "";
    }

    private void Select(string id, bool module)
    {
        var degree = module ? Graph.Graph.GetModuleDegree(id) : Graph.Graph.GetDegree(id);
        if (module) Modules.SelectedItem = id;
        SelectionInfo.Text = string.Format(CultureInfo.CurrentCulture, Text(module ? "SelectedModule" : "Selected"),
            id, degree.Incoming, degree.Outgoing, Text(Graph.Graph.Vertices.FirstOrDefault(v => v.Id == id)?.Kind.ToString() ?? Graph.Graph.GetKind(id).ToString()));
    }

    private void ModuleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Modules.SelectedItem is not string path) return;
        Graph.SelectedModule = path;
        var degree = Graph.Graph.GetModuleDegree(path);
        SelectionInfo.Text = string.Format(CultureInfo.CurrentCulture, Text("SelectedModule"), path, degree.Incoming, degree.Outgoing, Text(Graph.Graph.GetKind(path).ToString()));
        Graph.InvalidateVisual();
    }

    private void ResetClick(object sender, RoutedEventArgs e)
    {
        Graph.SetGraph(DiamondGraph.CreateDefault());
        Graph.Fit();
    }

    private void TypesExampleClick(object sender, RoutedEventArgs e)
    {
        var graph = DiamondGraph.CreateDefault();
        graph.Expand("root/L");
        graph.Expand("root/R");
        Graph.SetGraph(graph);
        Graph.Fit();
    }

    private void CollapseAllClick(object sender, RoutedEventArgs e)
    {
        Graph.Graph.CollapseAll();
        Graph.SelectedModule = null;
        Graph.Refresh();
    }

    private void CollapseClick(object sender, RoutedEventArgs e)
    {
        if (Modules.SelectedItem is not string path) return;
        Graph.Graph.Collapse(path);
        Graph.SelectedModule = null;
        Graph.Refresh();
    }

    private void GenerateClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(Budget.Text, out int budget) || budget < 0 || budget > DiamondGraph.MaximumExpansions)
        {
            Status.Text = Text("InvalidBudget");
            return;
        }
        try
        {
            Graph.Graph.Generate((int)DepthSlider.Value, (GenerationMode)Mode.SelectedIndex, budget);
            Graph.SelectedModule = null;
            Graph.Refresh();
            Graph.Fit();
        }
        catch (InvalidOperationException) { Status.Text = Text("InvalidBudget"); }
    }

    private void FitClick(object sender, RoutedEventArgs e) => Graph.Fit();
    private void ZoomInClick(object sender, RoutedEventArgs e) => Graph.Zoom(1.25);
    private void ZoomOutClick(object sender, RoutedEventArgs e) => Graph.Zoom(0.8);
}
