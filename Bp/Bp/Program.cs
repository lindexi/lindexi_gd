using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Bp
{
    // https://blog.csdn.net/lsgo_myp/article/details/88594907
    class Program
    {
        static void Main(string[] args)
        {
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


            ActivationNetwork network = new ActivationNetwork(
                new ThresholdFunction(), 2, 1);

            SlowLearning teacher = new SlowLearning(network);

            int iteration = 1;
            while (true)
            {
                double error = teacher.RunEpoch(inputs, outputs);
                Console.WriteLine($"迭代次数:{iteration},总体误差:{error} ");

                if (error == 0)
                    break;
                iteration++;
            }

            Console.WriteLine();




            //double[][] inputs = new double[4][];
            //double[][] outputs = new double[4][];

            //(0, 0); (0, 1); (1, 0)
            //inputs[0] = new double[] { 0, 0 };
            //inputs[1] = new double[] { 0, 1 };
            //inputs[2] = new double[] { 1, 0 };

            //outputs[0] = new double[] { 0 };
            //outputs[1] = new double[] { 0 };
            //outputs[2] = new double[] { 0 };

            //(1, 1)
            //inputs[3] = new double[] { 1, 1 };
            //outputs[3] = new double[] { 1 };


            //ActivationNetwork network = new ActivationNetwork(
            //    new ThresholdFunction(), 2, 1);

            //PerceptronLearning teacher = new PerceptronLearning(network);
            //teacher.LearningRate = 0.1;

            //int iteration = 1;
            //while (true)
            //{
            //    double error = teacher.RunEpoch(inputs, outputs);
            //    Console.WriteLine(@"迭代次数:{0},总体误差:{1}", iteration, error);

            //    if (error == 0)
            //        break;
            //    iteration++;
            //}

            //Console.WriteLine();

            //ActivationNeuron neuron = network.Layers[0].Neurons[0] as ActivationNeuron;

            //Console.WriteLine(@"Weight 1:{0}", neuron.Weights[0].ToString("F3"));
            //Console.WriteLine(@"Weight 2:{0}", neuron.Weights[1].ToString("F3"));
            //Console.WriteLine(@"Threshold:{0}", neuron.Threshold.ToString("F3"));

            //Console.Read();





            //double[][] inputs = new double[4][];
            //double[][] outputs = new double[4][];

            ////(0,0)
            //inputs[0] = new double[] { 0, 0 };
            //outputs[0] = new double[] { 0 };

            ////(1,1);(0,1);(1,0)
            //inputs[1] = new double[] { 0, 1 };
            //inputs[2] = new double[] { 1, 0 };
            //inputs[3] = new double[] { 1, 1 };

            //outputs[1] = new double[] { 1 };
            //outputs[2] = new double[] { 1 };
            //outputs[3] = new double[] { 1 };


            //ActivationNetwork network = new ActivationNetwork(new ThresholdFunction(), 2, 1);

            //SlowLearning teacher = new SlowLearning(network);

            //int iteration = 1;
            //while (true)
            //{
            //    double error = teacher.RunEpoch(inputs, outputs);
            //    Console.WriteLine(@"迭代次数:{0},总体误差:{1}", iteration, error);

            //    if (error == 0)
            //        break;
            //    iteration++;
            //}

            //Console.WriteLine();
            //ActivationNeuron neuron = network.Layers[0].Neurons[0] as ActivationNeuron;

            //Console.WriteLine(@"Weight 1:{0}", neuron.Weights[0].ToString("F3"));
            //Console.WriteLine(@"Weight 2:{0}", neuron.Weights[1].ToString("F3"));
            //Console.WriteLine(@"Threshold:{0}", neuron.Threshold.ToString("F3"));





            //double[][] inputs = new double[4][];
            //double[][] outputs = new double[4][];

            ////(0,0)
            //inputs[0] = new double[] { 0, 0 };
            //outputs[0] = new double[] { 0 };

            ////(1,1);(0,1);(1,0)
            //inputs[1] = new double[] { 0, 1 };
            //inputs[2] = new double[] { 1, 0 };
            //inputs[3] = new double[] { 1, 1 };

            //outputs[1] = new double[] { 1 };
            //outputs[2] = new double[] { 1 };
            //outputs[3] = new double[] { 1 };


            //ActivationNetwork network = new ActivationNetwork(
            //    new ThresholdFunction(), 2, 1);

            //PerceptronLearning teacher = new PerceptronLearning(network);
            //teacher.LearningRate = 0.1;

            //int iteration = 1;
            //while (true)
            //{
            //    double error = teacher.RunEpoch(inputs, outputs);
            //    Console.WriteLine(@"迭代次数:{0},总体误差:{1}", iteration, error);

            //    if (error == 0)
            //        break;
            //    iteration++;
            //}

            //Console.WriteLine();
            //ActivationNeuron neuron = network.Layers[0].Neurons[0] as ActivationNeuron;

            //Console.WriteLine(@"Weight 1:{0}", neuron.Weights[0].ToString("F3"));
            //Console.WriteLine(@"Weight 2:{0}", neuron.Weights[1].ToString("F3"));
            //Console.WriteLine(@"Threshold:{0}", neuron.Threshold.ToString("F3"));




            //double[][] inputs = new double[15][];
            //double[][] outputs = new double[15][];

            ////(0.1,0.1);(0.2,0.3);(0.3,0.4);(0.1,0.3);(0.2,0.5)
            //inputs[0] = new double[] { 0.1, 0.1 };
            //inputs[1] = new double[] { 0.2, 0.3 };
            //inputs[2] = new double[] { 0.3, 0.4 };
            //inputs[3] = new double[] { 0.1, 0.3 };
            //inputs[4] = new double[] { 0.2, 0.5 };

            //outputs[0] = new double[] { 1, 0, 0 };
            //outputs[1] = new double[] { 1, 0, 0 };
            //outputs[2] = new double[] { 1, 0, 0 };
            //outputs[3] = new double[] { 1, 0, 0 };
            //outputs[4] = new double[] { 1, 0, 0 };

            ////(0.1,1.0);(0.2,1.1);(0.3,0.9);(0.4,0.8);(0.2,0.9)
            //inputs[5] = new double[] { 0.1, 1.0 };
            //inputs[6] = new double[] { 0.2, 1.1 };
            //inputs[7] = new double[] { 0.3, 0.9 };
            //inputs[8] = new double[] { 0.4, 0.8 };
            //inputs[9] = new double[] { 0.2, 0.9 };

            //outputs[5] = new double[] { 0, 1, 0 };
            //outputs[6] = new double[] { 0, 1, 0 };
            //outputs[7] = new double[] { 0, 1, 0 };
            //outputs[8] = new double[] { 0, 1, 0 };
            //outputs[9] = new double[] { 0, 1, 0 };

            ////(1.0,0.4);(0.9,0.5);(0.8,0.6);(0.9,0.4);(1.0,0.5)
            //inputs[10] = new double[] { 1.0, 0.4 };
            //inputs[11] = new double[] { 0.9, 0.5 };
            //inputs[12] = new double[] { 0.8, 0.6 };
            //inputs[13] = new double[] { 0.9, 0.4 };
            //inputs[14] = new double[] { 1.0, 0.5 };

            //outputs[10] = new double[] { 0, 0, 1 };
            //outputs[11] = new double[] { 0, 0, 1 };
            //outputs[12] = new double[] { 0, 0, 1 };
            //outputs[13] = new double[] { 0, 0, 1 };
            //outputs[14] = new double[] { 0, 0, 1 };

            //ActivationNetwork network = new ActivationNetwork(new ThresholdFunction(), 2, 3);

            //SlowLearning teacher = new SlowLearning(network);

            //int iteration = 1;

            //while (true)
            //{
            //    double error = teacher.RunEpoch(inputs, outputs);
            //    Console.WriteLine(@"迭代次数:{0},总体误差:{1}", iteration, error);

            //    if (error == 0)
            //        break;
            //    iteration++;
            //}






            //double[][] inputs = new double[15][];
            //double[][] outputs = new double[15][];

            ////(0.1,0.1);(0.2,0.3);(0.3,0.4);(0.1,0.3);(0.2,0.5)
            //inputs[0] = new double[] { 0.1, 0.1 };
            //inputs[1] = new double[] { 0.2, 0.3 };
            //inputs[2] = new double[] { 0.3, 0.4 };
            //inputs[3] = new double[] { 0.1, 0.3 };
            //inputs[4] = new double[] { 0.2, 0.5 };

            //outputs[0] = new double[] { 1, 0, 0 };
            //outputs[1] = new double[] { 1, 0, 0 };
            //outputs[2] = new double[] { 1, 0, 0 };
            //outputs[3] = new double[] { 1, 0, 0 };
            //outputs[4] = new double[] { 1, 0, 0 };

            ////(0.1,1.0);(0.2,1.1);(0.3,0.9);(0.4,0.8);(0.2,0.9)
            //inputs[5] = new double[] { 0.1, 1.0 };
            //inputs[6] = new double[] { 0.2, 1.1 };
            //inputs[7] = new double[] { 0.3, 0.9 };
            //inputs[8] = new double[] { 0.4, 0.8 };
            //inputs[9] = new double[] { 0.2, 0.9 };

            //outputs[5] = new double[] { 0, 1, 0 };
            //outputs[6] = new double[] { 0, 1, 0 };
            //outputs[7] = new double[] { 0, 1, 0 };
            //outputs[8] = new double[] { 0, 1, 0 };
            //outputs[9] = new double[] { 0, 1, 0 };

            ////(1.0,0.4);(0.9,0.5);(0.8,0.6);(0.9,0.4);(1.0,0.5)
            //inputs[10] = new double[] { 1.0, 0.4 };
            //inputs[11] = new double[] { 0.9, 0.5 };
            //inputs[12] = new double[] { 0.8, 0.6 };
            //inputs[13] = new double[] { 0.9, 0.4 };
            //inputs[14] = new double[] { 1.0, 0.5 };

            //outputs[10] = new double[] { 0, 0, 1 };
            //outputs[11] = new double[] { 0, 0, 1 };
            //outputs[12] = new double[] { 0, 0, 1 };
            //outputs[13] = new double[] { 0, 0, 1 };
            //outputs[14] = new double[] { 0, 0, 1 };

            //ActivationNetwork network = new ActivationNetwork(new ThresholdFunction(), 2, 3);

            //PerceptronLearning teacher = new PerceptronLearning(network);
            //teacher.LearningRate = 0.1;

            //int iteration = 1;

            //while (true)
            //{
            //    double error = teacher.RunEpoch(inputs, outputs);
            //    Console.WriteLine(@"迭代次数:{0},总体误差:{1}", iteration, error);

            //    if (error == 0)
            //        break;
            //    iteration++;
            //}


            Console.Read();

        }
    }
}
