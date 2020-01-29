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

        //网络输入的个数 
        public int InputsCount { get; }

        // 构成网络的层
        public Layer[] Layers { get; }

        // 网络的输出向量
        public double[] Output { get; protected set; }

        // 构造函数 
        protected Network(int inputsCount, int layersCount)
        {
            InputsCount = Math.Max(1, inputsCount);
            LayersCount = Math.Max(1, layersCount);
            Layers = new Layer[LayersCount];
        }

        // 计算网络的输出
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

        // 初始化整个网络的权值
        public virtual void Randomize()
        {
            foreach (Layer layer in Layers)
            {
                layer.Randomize();
            }
        }
    }








































































































}
