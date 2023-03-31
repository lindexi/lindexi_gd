﻿using System.Windows;

namespace LightTextEditorPlus.Utils;

/// <summary>
/// 对 Size 的扩展
/// </summary>
public static class SizeExtension
{
    /// <summary>
    /// 从文本的 Size 类型转换为 WPF 的 Size 类型
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public static Size ToWpfSize(this LightTextEditorPlus.Core.Primitive.Size size) => new Size(size.Width, size.Height);
}