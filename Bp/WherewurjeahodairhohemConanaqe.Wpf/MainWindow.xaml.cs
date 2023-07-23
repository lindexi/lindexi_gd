using Microsoft.Msagl.Drawing;

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
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Shape = Microsoft.Msagl.Drawing.Shape;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Core.Layout;

namespace WherewurjeahodairhohemConanaqe.Wpf;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Graph graph = new Graph();

        for (int i = 0; i < 1; i++)
        {
            var edge = graph.AddEdge("Octagon" + i, "Label", "Hexagon" + i);
            graph.AddPrecalculatedEdge(edge);
            graph.FindNode("Octagon" + i).Attr.Shape = Shape.Box;
            graph.FindNode("Hexagon" + i).Attr.Shape = Shape.Hexagon;

            graph.FindNode("Octagon" + i).Attr.FillColor = Microsoft.Msagl.Drawing.Color.Blue;
        }


        var railGraph = new RailGraph();

        

        graph.Attr.LayerDirection = LayerDirection.LR;
        GraphControl.Graph = graph;

    }
}
