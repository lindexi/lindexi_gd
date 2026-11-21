using System.Windows;
using System.Windows.Controls;

namespace OrderedNodeReplacement.Wpf;

public partial class MainWindow : Window
{
    /// <summary>Initializes the window with the three-node example selected.</summary>
    public MainWindow()
    {
        InitializeComponent();
        TemplateSelector.SelectedIndex = 0;
    }

    private void OnTemplateChanged(object sender, SelectionChangedEventArgs e)
    {
        var example = TemplateSelector.SelectedIndex switch
        {
            0 => ReplacementExample.ThreeNode(),
            1 => ReplacementExample.FiveNode(),
            _ => null
        };
        if (example is null)
        {
            return;
        }

        var expanded = example.Template.Apply(example.Graph, example.Target);
        var templateIds = example.Template.Fragment.Nodes
            .Select(node => example.Target.Child(node.Id.Name)).ToHashSet();
        var expectedIds = example.Graph.Nodes.Where(node => node.Id != example.Target)
            .Select(node => node.Id).Concat(templateIds).ToHashSet();
        var adapters = expanded.Nodes.Count(node => !expectedIds.Contains(node.Id));

        GraphDiagram.Draw(BeforeDiagram, example.Graph, example.BeforeLayout, id => id == example.Target);
        GraphDiagram.Draw(AfterDiagram, expanded, example.AfterLayout, templateIds.Contains);
        NodeCounts.Text = $"{example.Graph.Nodes.Count} → {expanded.Nodes.Count}";
        EdgeCounts.Text = $"{example.Graph.Edges.Count} → {expanded.Edges.Count}";
        AdapterCount.Text = adapters.ToString();
        AdapterNote.Visibility = adapters == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
