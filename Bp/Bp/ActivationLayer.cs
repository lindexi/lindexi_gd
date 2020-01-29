namespace Bp
{
    public class ActivationLayer : Layer
    {
        /// <summary>
        /// 神经网络层
        /// </summary>
        /// <param name="neuronsCount">该层神经元的个数</param>
        /// <param name="inputsCount">该层的输入个数</param>
        /// <param name="function">激活函数</param>
        public ActivationLayer(int neuronsCount, int inputsCount, IActivationFunction function)
            : base(neuronsCount, inputsCount)
        {
            for (int i = 0; i < Neurons.Length; i++)
                Neurons[i] = new ActivationNeuron(inputsCount, function);
        }

        public void SetActivationFunction(IActivationFunction function)
        {
            for (int i = 0; i < Neurons.Length; i++)
            {
                ((ActivationNeuron)Neurons[i]).ActivationFunction = function;
            }
        }
    }








































































































}
