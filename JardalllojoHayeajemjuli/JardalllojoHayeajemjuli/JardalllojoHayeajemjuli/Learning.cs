using System;
using System.Diagnostics;

namespace JardalllojoHayeajemjuli
{
    /// <summary>
    /// 训练方法
    /// </summary>
    public class Learning
    {
        public Neuron Learn(Data[] data)
        {
            // 一个神经元就足够了
            var neuron = new Neuron(2);

            // 误差
            var error = double.MaxValue;
            var n = 0;

            while (error != 0)
            {
                n++;
                Debug.WriteLine($"第{n}次训练");
                error = 0;
                
                // 修改权值再来一次
                neuron.Randomize();
                foreach (var temp in data)
                {
                    var result = neuron.Compute(temp.Input);

                    // 将计算出来的结果和预期的相减去，就那到了误差
                    error += Math.Abs(temp.Output - result);
                }
            }

            return neuron;
        }
    }
}