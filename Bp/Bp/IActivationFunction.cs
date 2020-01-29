using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Bp
{
    /// <summary>
    /// 对激活函数的抽象
    /// 所有与神经元一起使用的激活函数，都应该实现这个接口，这些函数将神经元的输出作为其输入加权和的函数来计算
    /// </summary>
    public interface IActivationFunction
    {
        /// <summary>
        /// 激活函数
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        double Function(double x);

        /// <summary>
        /// 求x点的导数
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        double DerivativeX(double x);

        /// <summary>
        /// 求y点的导数
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        double DerivativeY(double y);
    }








































































































}
