using System;

namespace Bp
{
    public class SlowLearning : ISupervisedLearning
    {
        public SlowLearning(ActivationNetwork network)
        {
            _network = network;
        }

        /// <summary>
        /// 神经网络
        /// </summary>
        private readonly ActivationNetwork _network;

        private DoubleRange RandRange { get; } = new DoubleRange(-1.0, 1.0);

        /// <summary>
        /// 单个训练样本
        /// </summary> 
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public double Run(double[] input, double[] output)
        {
            double[] networkOutput = _network.Compute(input);
            Layer layer = _network.Layers[0];
            // 统计总误差
            double error = 0.0;

            for (int j = 0; j < layer.Neurons.Length; j++)
            {
                // 误差值，用已知的值减去网络计算出来的值
                double e = output[j] - networkOutput[j];
                // 如果存在误差，那么更新权重
                if (e != 0)
                {
                    ActivationNeuron perceptron = (ActivationNeuron)layer.Neurons[j];

                    for (int i = 0; i < perceptron.Weights.Length; i++)
                    {
                        perceptron.Weights[i] = RandRange.GetRan();
                    }

                    perceptron.Threshold = RandRange.GetRan();

                    error += Math.Abs(e);
                }
            }

            return error;
        }

        /// <summary>
        /// 训练所有样本
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public double RunEpoch(double[][] input, double[][] output)
        {
            double error = 0.0;
            for (int i = 0, n = input.Length; i < n; i++)
            {
                error += Run(input[i], output[i]);
            }
            return error;
        }
    }
}
