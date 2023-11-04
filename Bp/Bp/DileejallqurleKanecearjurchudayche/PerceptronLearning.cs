using System;

namespace Bp
{
    /// <summary>
    /// 学习规则
    /// </summary>
    //感知器学习是有监督学习，所以要实现 ISuperviseLearning 接口
    // https://www.jianshu.com/p/f48adf579a9e
    public class PerceptronLearning : ISupervisedLearning
    {
        // 神经网络
        private readonly ActivationNetwork _network;

        /// <summary>
        /// 学习率
        /// </summary>
        private double _learningRate = 0.1;

        /// <summary>
        /// 学习率, [0, 1]
        /// </summary>
        public double LearningRate
        {
            get { return _learningRate; }
            set
            {
                _learningRate = Math.Max(0.0, Math.Min(1.0, value));
            }
        }

        //https://blog.csdn.net/red_stone1/article/details/80491895
        public PerceptronLearning(ActivationNetwork network)
        {
            if (network.Layers.Length != 1)
            {
                throw new ArgumentException("无效的神经网络，它应该只有一层。");
            }

            _network = network;
        }

        /// <summary>
        /// 单个训练样本
        /// </summary> 
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public double Run(double[] input, double[] output)
        {
            // 网络对单个输入拿到对应的输出值
            double[] networkOutput = _network.Compute(input);
            Layer layer = _network.Layers[0];
            // 统计总误差
            double error = 0.0;

            // https://blog.csdn.net/qq_42442369/article/details/87613450
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
                        // 用学习率乘以误差乘以输入值更新值
                        perceptron.Weights[i] += _learningRate * e * input[i];
                    }
                    perceptron.Threshold += _learningRate * e;

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
