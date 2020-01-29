namespace Bp
{
    /// <summary>
    /// 对非监督学习的抽象，该接口描述了所有非监督学习算法都应该实现的方法
    /// </summary>
    public interface IUnsupervisedLearning
    {
        /// <summary>
        /// 单样本训练
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        double Run(double[] input);

        /// <summary>
        /// 多样本训练
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        double RunEpoch(double[][] input);
    }



   

  

  







 








































































































}
