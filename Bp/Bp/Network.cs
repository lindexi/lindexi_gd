using System;


namespace Bp
{
    /// <summary>
    /// 对神经网络结构的抽象，它表示神经元层的集合
    /// </summary>
    public abstract class Network
    {
        /// <summary>
        /// 网络层的个数
        /// </summary>
        protected int LayersCount { set; get; }

        /// <summary>
        /// 网络输入的个数
        /// </summary>
        public int InputsCount { get; }

        /// <summary>
        /// 构成网络的层
        /// </summary>
        public Layer[] Layers { get; }

        /// <summary>
        /// 网络的输出向量
        /// </summary>
        public double[] Output { get; protected set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="inputsCount">输入的个数</param>
        /// <param name="layersCount">网络层的数量</param>
        protected Network(int inputsCount, int layersCount)
        {
            InputsCount = Math.Max(1, inputsCount);
            LayersCount = Math.Max(1, layersCount);
            Layers = new Layer[LayersCount];
        }

        /// <summary>
        /// 计算网络的输出
        /// </summary>
        /// <param name="input">要求输入的元素数和 InputsCount 网络输入的个数相同</param>
        /// <returns></returns>
        public virtual double[] Compute(double[] input)
        {
            // 第一层用于输入，将输入层作为传输
            double[] courier = input;

            for (int i = 0; i < Layers.Length; i++)
            {
                // 第一层的输出作为第二层的输入
                // 所以输入是 courier 而返回的输出作为下一层的输入
                courier = Layers[i].Compute(courier);
            }
            // 最后一层的输出作为网络输出
            Output = courier;
            return courier;
        }

        /// <summary>
        /// 初始化整个网络的权值
        /// </summary>
        public virtual void Randomize()
        {
            foreach (Layer layer in Layers)
            {
                layer.Randomize();
            }
        }
    }
}
