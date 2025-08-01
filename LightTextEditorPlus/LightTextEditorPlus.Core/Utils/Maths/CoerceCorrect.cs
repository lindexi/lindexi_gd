using System;

namespace LightTextEditorPlus.Core.Utils.Maths
{
    /// <summary>
    /// 做强制检查的辅助类
    /// </summary>
    internal static class CoerceCorrect
    {
        /// <summary>
        /// 使给定值限定在最大与最小范围内
        /// </summary>
        public static int CoerceValue(this int value, int minimum, int maximum)
        {
            // 这个方法实现和 Math.Clamp 有什么区别？ 核心差别是 maximum 比 minimum 小的时候，Math.Clamp 会抛出异常，而这个方法不会
            //Math.Clamp()
            // - 如果 min ≤ value ≤ max，则为 value
            // - 如果 value<min，则为 min
            // - 如果 value>max，则为 max
            return Math.Max(Math.Min(value, maximum), minimum);
        }

        /// <summary>
        /// 使给定值限定在最大与最小范围内
        /// </summary>
        public static double CoerceValue(this double value, double minimum, double maximum)
        {
            return Math.Max(Math.Min(value, maximum), minimum);
        }
    }
}
