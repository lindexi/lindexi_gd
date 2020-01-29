using System;

namespace Bp
{
    /// <summary>
    /// 神经元
    /// </summary>
    public class ActivationNeuron : Neuron
    {
        /// <summary>
        /// 阈值
        /// </summary>
        public double Threshold { get; set; } = 0.0;

        /// <summary>
        /// 激活函数
        /// </summary>
        public IActivationFunction ActivationFunction { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="inputs">输入个数</param>
        /// <param name="function">激活函数</param>
        public ActivationNeuron(int inputs, IActivationFunction function)
            : base(inputs)
        {
            ActivationFunction = function;
        }

        /// <summary>
        /// 初始化权值阈值
        /// </summary>
        public override void Randomize()
        {
            base.Randomize();
            Threshold = RandRange.GetRan();
        }

        // 计算神经元的输出
        public override double Compute(double[] input)
        {
            if (input.Length != InputsCount)
                throw new ArgumentException("输入向量的长度错误。");

            double sum = 0.0;
            for (int i = 0; i < Weights.Length; i++)
            {
                sum += Weights[i] * input[i];
            }

            sum += Threshold;
            double output = ActivationFunction.Function(sum);
            Output = output;
            return output;
        }
    }








































































































}
