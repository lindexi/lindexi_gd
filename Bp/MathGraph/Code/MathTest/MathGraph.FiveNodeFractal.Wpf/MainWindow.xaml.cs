using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MathGraph.FiveNodeFractal.Wpf.Model;

namespace MathGraph.FiveNodeFractal.Wpf;

/// <summary>Controls the finite five-node topology experiment.</summary>
public partial class MainWindow : Window
{
    private bool ready;
    /// <summary>Initializes the base FullCross graph and localized controls.</summary>
    public MainWindow()
    {
        InitializeComponent();
        ready = true;
        Graph.Changed += Update;
        Graph.Rejected += () => Status.Text = Resource("Failure");
        Graph.Selected += (path, module) =>
        {
            ShowSelection(path);
            if (module) Modules.SelectedItem = path;
        };
        Update();
    }
    private string Resource(string key) => (string)FindResource(key);
    private Prototype CurrentPrototype => (Prototype)Pattern.SelectedIndex;
    private void Update()
    {
        var graph = Graph.Graph;
        Stats.Text = string.Format(CultureInfo.CurrentCulture, Resource("Stats"), graph.RealVertexCount, graph.Edges.Count, graph.ExpandedPaths.Count, graph.Depth);
        string? selected = Graph.SelectedModule;
        Modules.ItemsSource = graph.ExpandedPaths;
        Modules.SelectedItem = selected;
    }
    private void ShowSelection(string path)
    {
        var degree = Graph.Graph.GetSignature(path);
        Selection.Text = string.Format(CultureInfo.CurrentCulture, Resource("Selected"), path, degree.Incoming, degree.Outgoing);
    }
    private void Execute(Action action)
    {
        try { action(); Status.Text = ""; Graph.Refresh(); }
        catch (ArgumentException) { Status.Text = Resource("Failure"); }
        catch (InvalidOperationException) { Status.Text = Resource("Failure"); }
    }
    private void PatternChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready) return;
        Graph.SetGraph(new(CurrentPrototype)); Selection.Text = ""; Status.Text = "";
    }
    private void Example(string path) => Execute(() =>
    {
        var graph = new FiveNodeGraph(CurrentPrototype);
        graph.Expand(path);
        Graph.SetGraph(graph);
        ShowSelection(path);
    });
    private void ExampleCClick(object sender, RoutedEventArgs e) => Example("root/C");
    private void ExampleAClick(object sender, RoutedEventArgs e) => Example("root/A");
    private void CollapseAllClick(object sender, RoutedEventArgs e) => Execute(() => { Graph.Graph.CollapseAll(); Graph.SelectedModule = null; Selection.Text = ""; Graph.Fit(); });
    private void GenerateClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(Levels.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out int depth) || depth < 0)
        {
            Status.Text = Resource("InvalidDepth");
            return;
        }
        Execute(() => { Graph.Graph.Generate(depth); Graph.SelectedModule = null; Selection.Text = ""; Graph.Fit(); });
    }
    private void CollapseClick(object sender, RoutedEventArgs e)
    {
        if (Modules.SelectedItem is string path) Execute(() => { Graph.Graph.Collapse(path); Graph.SelectedModule = null; Selection.Text = ""; });
    }
    private void ModuleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || Modules.SelectedItem is not string path) return;
        Graph.SelectedModule = path; ShowSelection(path); Graph.InvalidateVisual();
    }
    private void ZoomInClick(object sender, RoutedEventArgs e) => Graph.Zoom(1.3);
    private void ZoomOutClick(object sender, RoutedEventArgs e) => Graph.Zoom(1 / 1.3);
    private void FitClick(object sender, RoutedEventArgs e) => Graph.Fit();
}
