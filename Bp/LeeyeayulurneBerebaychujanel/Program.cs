// See https://aka.ms/new-console-template for more information

using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Routing;


var geometryGraph = new GeometryGraph();

foreach (string id in "0 1 2 3 4 5 6 A B C D E F G a b c d e".Split(' '))
{
    var node = new Node(CurveFactory.CreateRectangle(10, 10, new Point(100,100)),id);
    geometryGraph.Nodes.Add(node);
}

var graph = geometryGraph;

graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("B")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("C")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("D")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("E")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("B"), graph.FindNodeByUserData("E")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("F")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("0"), graph.FindNodeByUserData("F")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("1"), graph.FindNodeByUserData("F")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("2"), graph.FindNodeByUserData("F")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("3"), graph.FindNodeByUserData("F")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("4"), graph.FindNodeByUserData("F")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("5"), graph.FindNodeByUserData("F")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("6"), graph.FindNodeByUserData("F")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("a"), graph.FindNodeByUserData("b")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("b"), graph.FindNodeByUserData("c")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("c"), graph.FindNodeByUserData("d")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("d"), graph.FindNodeByUserData("e")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("a")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("B"), graph.FindNodeByUserData("a")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("C"), graph.FindNodeByUserData("a")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("a")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("E"), graph.FindNodeByUserData("a")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("F"), graph.FindNodeByUserData("a")));
graph.Edges.Add(new Edge(graph.FindNodeByUserData("G"), graph.FindNodeByUserData("a")));

//foreach (var geometryGraphNode in geometryGraph.Nodes)
//{

//    geometryGraphNode.BoundingBox = new Rectangle(new Size(10, 10), new Point(10, 10));
//}

//geometryGraph.BoundingBox = new Rectangle(new Size(100, 100), new Point(10, 10));

var layeredLayout = new LayeredLayout(geometryGraph,new SugiyamaLayoutSettings()
{
    EdgeRoutingSettings = { EdgeRoutingMode = EdgeRoutingMode.Spline }
});
layeredLayout.Run();


Console.WriteLine("Hello, World!");
