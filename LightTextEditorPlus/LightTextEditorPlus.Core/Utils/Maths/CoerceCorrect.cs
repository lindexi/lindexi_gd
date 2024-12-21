using System;

namespace LightTextEditorPlus.Core.Utils.Maths
{
    /// <summary>
    /// 做强制检查的辅助类
    /// </summary>
    public static class CoerceCorrect
    {
        /// <summary>
        /// 使给定值限定在最大与最小范围内
        /// </summary>
        public static int CoerceValue(this int value, int minimum, int maximum)
        {
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
