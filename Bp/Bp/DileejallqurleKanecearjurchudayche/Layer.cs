using System;

namespace Bp
{
    /// <summary>
    /// 对神经网络层的抽象，代表该层神经元的集合
    /// </summary>
    public abstract class Layer
    {
        /// <summary>
        /// 该层神经元的个数
        /// </summary>
        protected int NeuronsCount { set; get; }

        /// <summary>
        /// 该层的输入个数
        /// </summary>
        public int InputsCount { get; }

        /// <summary>
        /// 该层神经元的集合
        /// </summary>
        public Neuron[] Neurons { get; }

        /// <summary>
        /// 该层的输出向量
        /// </summary>
        public double[] Output { get; protected set; }

        /// <summary>
        /// 神经网络层的抽象
        /// </summary>
        /// <param name="neuronsCount">该层神经元的个数</param>
        /// <param name="inputsCount">该层的输入个数</param>
        /// 每一层的神经元的个数不同，同一层的每个神经元的输入个数相同，但不同层的输入个数可以不同
        protected Layer(int neuronsCount, int inputsCount)
        {
            InputsCount = Math.Max(1, inputsCount);
            NeuronsCount = Math.Max(1, neuronsCount);
            Neurons = new Neuron[NeuronsCount];
        }

        /// <summary>
        /// 计算该层的输出向量
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual double[] Compute(double[] input)
        {
            double[] output = new double[NeuronsCount];
            for (int i = 0; i < Neurons.Length; i++)
                output[i] = Neurons[i].Compute(input);

            Output = output;
            return output;
        }

        /// <summary>
        /// 初始化该层神经元的权值
        /// </summary>
        public virtual void Randomize()
        {
            foreach (Neuron neuron in Neurons)
            {
                neuron.Randomize();
            }
        }
    }
}
