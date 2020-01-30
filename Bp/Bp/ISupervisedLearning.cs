namespace Bp
{
    /// <summary>
    /// 对监督学习的抽象，这个接口描述了所有监督学习算法应该实现的方法
    /// </summary>
    /// 监督学习和下面的非监督学习的不同在于，非监督学习只需要给输入，不需要给输出，也就是在训练是不需要知道结果，而监督学习是需要知道输入是什么对应输出是什么，相对于非监督学习，理解监督学习比较简单
    public interface ISupervisedLearning
    {
        /// <summary>
        /// 单样本训练
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        double Run(double[] input, double[] output);

        /// <summary>
        /// 多样本训练
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        double RunEpoch(double[][] input, double[][] output);
    }
}
