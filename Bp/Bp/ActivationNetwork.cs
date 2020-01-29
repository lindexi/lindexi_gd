namespace Bp
{
    public class ActivationNetwork : Network
    {
        /// <summary>
        /// 神经网络
        /// </summary>
        /// <param name="function"></param>
        /// <param name="inputsCount"></param>
        /// <param name="neuronsCount">指定神经网络每层中的神经元数量</param>
        public ActivationNetwork(IActivationFunction function, int inputsCount, params int[] neuronsCount)
            : base(inputsCount, neuronsCount.Length)
        {
            // neuronsCount 指定神经网络每层中的神经元数量。
            for (int i = 0; i < Layers.Length; i++)
            {
                Layers[i] = new ActivationLayer(
                    neuronsCount[i],
                    // 每个神经元只有一个输出，上一层有多少个神经元也就有多少个输出，也就是这一层需要有多少输入
                    (i == 0) ? inputsCount : neuronsCount[i - 1],
                    function);
            }
        }

        public void SetActivationFunction(IActivationFunction function)
        {
            for (int i = 0; i < Layers.Length; i++)
            {
                ((ActivationLayer)Layers[i]).SetActivationFunction(function);
            }
        }
    }








































































































}
