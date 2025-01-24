using System;

namespace LightTextEditorPlus.Core.Utils.Maths;

/// <summary>
/// 用于处理数值计算近似性的辅助类。
/// tolerance默认值为0.00001
/// </summary>
static partial class Nearly
{
    /// <summary>
    /// 判断两个 <see cref="double"/> 值是否近似相等。
    /// </summary>
    /// <param name="d1">值1。</param>
    /// <param name="d2">值2。</param>
    /// <param name="tolerance">近似容差。</param>
    public static bool Equals(double d1, double d2, double tolerance = AlmostZero)
    {
        return Math.Abs(d1 - d2) < tolerance;
    }

    /// <summary>
    /// 默认容差。
    /// </summary>
    /// 尽管这不是 2 的倍数，但保持兼容，也不改
    internal const double AlmostZero = 0.00001;
}
