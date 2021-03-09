using System.Windows;

namespace Eraser
{
    static class VectorExtensions
    {
        /// <summary>
        /// 获取向量的 cos（θ）值
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double GetCos(this Vector vector)
            => vector.Y / vector.Length;

        /// <summary>
        /// 获取向量的 sin（θ）值
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double GetSin(this Vector vector)
            => vector.X / vector.Length;
    }
}