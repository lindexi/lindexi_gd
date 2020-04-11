using System;

namespace JardalllojoHayeajemjuli
{
    /// <summary>
    /// 神经元
    /// </summary>
    public class Neuron
    {
        /// <summary>
        /// 随机数范围
        /// </summary>
        public static DoubleRange RandRange { get; set; } = new DoubleRange(-1.0f, 1.0f);

        /// <summary>
        /// 多输入
        /// </summary>
        public int InputsCount { get; }

        /// <summary>
        /// 阈值
        /// </summary>
        public double Threshold { get; set; } = 0.0;

        /// <summary>
        /// 单输出
        /// </summary>
        public double Output { get; protected set; } = 0.0;

        /// <summary>
        /// 权值数组，每个输入对应一个权值，也就是 InputsCount 的数量和 Weights 元素数相同
        /// </summary>
        /// 在神经网络的每个元可以收到多个输入，而对每个输入需要使用不同的权值计算。此时对应每个输入一个权值，而输出只有一个
        public double[] Weights { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="inputs">表示有多少个输入</param>
        public Neuron(int inputs)
        {
            // 输入的数量不能小于1的值
            InputsCount = Math.Max(1, inputs);
            Weights = new double[InputsCount];
            Randomize();
        }

        /// <summary>
        /// 初始化权值
        /// </summary>
        public void Randomize()
        {
            for (int i = 0; i < InputsCount; i++)
            {
                // 创建在 RandRange.Max 和 RandRange.Min 范围内的随机数
                Weights[i] = RandRange.GetRan();
            }

            Threshold = RandRange.GetRan();
        }

        /// <summary>
        /// 对当前传入的输入计算输出的值
        /// </summary>
        /// <param name="input">输入的值的数量要求和 InputsCount 相同</param>
        /// <returns></returns>
        public double Compute(double[] input)
        {
            double sum = 0.0;
            for (int i = 0; i < Weights.Length; i++)
            {
                sum += Weights[i] * input[i];
            }

            sum += Threshold;

            // 通过阈值函数将输出规约到 1 和 0 两个值
            var thresholdFunction = new ThresholdFunction();
            double output = thresholdFunction.Function(sum);
            Output = output;
            return output;
        }
    }
}