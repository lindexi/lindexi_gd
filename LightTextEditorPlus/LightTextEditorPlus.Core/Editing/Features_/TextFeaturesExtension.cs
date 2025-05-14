using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace LightTextEditorPlus.Core.Editing;

/// <summary>
/// 功能特性扩展方法
/// </summary>
public static class TextFeaturesExtension
{
    /// <summary>
    /// 启用功能
    /// </summary>
    /// <param name="currentFeatures"></param>
    /// <param name="features"></param>
    /// <returns></returns>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextFeatures EnableFeatures(this TextFeatures currentFeatures, TextFeatures features)
    {
        return currentFeatures | features;
    }

    /// <summary>
    /// 禁用功能
    /// </summary>
    /// <param name="currentFeatures"></param>
    /// <param name="features"></param>
    /// <returns></returns>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextFeatures DisableFeatures(this TextFeatures currentFeatures, TextFeatures features)
    {
        return currentFeatures & ~features;
    }

    /// <summary>
    /// 是否启用功能
    /// </summary>
    /// <param name="currentFeatures"></param>
    /// <param name="features"></param>
    /// <returns></returns>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFeaturesEnable(this TextFeatures currentFeatures, TextFeatures features)
    {
        return (currentFeatures & features) == features;
    }
}