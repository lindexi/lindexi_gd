using Microsoft.Msagl.Drawing;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using WherewurjeahodairhohemConanaqe.Wpf.Core;

namespace WherewurjeahodairhohemConanaqe.Wpf;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var perceptronLearning = new PerceptronLearning();
        perceptronLearning.Run();

        var runner = new Runner(new NeuronManager());
        runner.Run();

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

class PerceptronLearning
{
    public NeuronManager NeuronManager { get; } = new NeuronManager();

    public void Run()
    {
        var runner = new Runner(NeuronManager);

        double[][] inputs = new double[4][];
        double[][] outputs = new double[4][];

        //(0,0);(0,1);(1,0)
        inputs[0] = new double[] { 0, 0 };
        inputs[1] = new double[] { 0, 1 };
        inputs[2] = new double[] { 1, 0 };

        outputs[0] = new double[] { 0 };
        outputs[1] = new double[] { 0 };
        outputs[2] = new double[] { 0 };

        //(1,1)
        inputs[3] = new double[] { 1, 1 };
        outputs[3] = new double[] { 1 };

        int n = 0;
        while (n < (inputs.Length * 2))
        {
            double e = 0d;
            for (var i = 0; i < inputs.Length; i++)
            {
                var input = inputs[i];
                var output = outputs[i];

                NeuronManager.InputNeuron.SetInput(new InputArgument(input));
                runner.Run();

                var outputArgument = NeuronManager.OutputNeuron.OutputArgument;

                var currentValue = GetE(output, outputArgument.Value);
                e += currentValue;

                //Debug.WriteLine($"{input[0]:0} {input[1]:0} {output[0]:0} [{outputArgument.Value[0]:0}]");

                if (currentValue < 0.01)
                {
                    n++;
                }
                else
                {
                    n = 0;
                }
            }

            NeuronManager.Learning(e);

            // 计算误差
            static double GetE(double[] output,IReadOnlyList<double> outputArgument)
            {
                if (outputArgument.Count < output.Length)
                {
                    return 1;
                }

                var sum = 0d;
                for (var i = 0; i < output.Length; i++)
                {
                    sum += Math.Abs(outputArgument[i] - output[i]);
                }
                return sum;
            }
        }
    }
}