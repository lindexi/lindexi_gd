using GraphX.Common.Models;
using GraphX.Controls;
using QuickGraph;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GraphX.Controls.Models;
using System.Windows.Markup;
using GraphX.Common.Interfaces;
using GraphX.Logic.Models;

namespace JalowawerenearniQebemwawiyo.Wpf;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var graphArea = GraphArea;
        graphArea.LogicCore = new LogicCoreExample();
        graphArea.VertexSelected += GraphArea_VertexSelected;
        graphArea.EdgeSelected += GraphArea_EdgeSelected;

        for (int i = 0; i < 2; i++)
        {
            var dataVertex = new DataVertex("Data" + i);

            var vc = new VertexControl(dataVertex);
            //vc.VCPRoot.Children.Add(new TextBlock()
            //{
            //    Text = "TextBlock" + i
            //});
            graphArea.AddVertexAndData(dataVertex, vc, true);
        }
 
        for (int i = 0; i < 1; i++)
        {
            var dataEdge = new DataEdge(graphArea.VertexList.First().Key, graphArea.VertexList.Last().Key);

            graphArea.InsertEdgeAndData(dataEdge, new EdgeControl(graphArea.VertexList.First().Value, graphArea.VertexList.Last().Value, dataEdge));
        }

    }


    private void GraphArea_EdgeSelected(object sender, EdgeSelectedEventArgs args)
    {

    }

    private void GraphArea_VertexSelected(object sender, VertexSelectedEventArgs args)
    {

    }
}

public class LogicCoreExample : GXLogicCore<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>>
{

}

public class GraphAreaExample : GraphArea<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> { }
public class GraphExample : BidirectionalGraph<DataVertex, DataEdge> { }
public class DataEdge : EdgeBase<DataVertex>
{
    /// <summary>
    /// Default constructor. We need to set at least Source and Target properties of the edge.
    /// </summary>
    /// <param name="source">Source vertex data</param>
    /// <param name="target">Target vertex data</param>
    /// <param name="weight">Optional edge weight</param>
    public DataEdge(DataVertex source, DataVertex target, double weight = 1)
        : base(source, target, weight)
    {
    }
    /// <summary>
    /// Default parameterless constructor (for serialization compatibility)
    /// </summary>
    public DataEdge()
        : base(null, null, 1)
    {
    }

    /// <summary>
    /// Custom string property for example
    /// </summary>
    public string Text { get; set; }

    #region GET members
    public override string ToString()
    {
        return Text;
    }

    #endregion
}

public class DataVertex : VertexBase
{
    /// <summary>
    /// Some string property for example purposes
    /// </summary>
    public string Text { get; set; }

    #region Calculated or static props

    public override string ToString()
    {
        return Text;
    }

    #endregion

    /// <summary>
    /// Default parameterless constructor for this class
    /// (required for YAXLib serialization)
    /// </summary>
    public DataVertex() : this(string.Empty)
    {
    }

    public DataVertex(string text = "")
    {
        Text = text;
    }
}