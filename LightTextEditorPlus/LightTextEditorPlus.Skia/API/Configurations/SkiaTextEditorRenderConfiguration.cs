using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Configurations;

public readonly record struct SkiaTextEditorRenderConfiguration()
{
    /// <summary>
    /// 使用逐字渲染方法。渲染效率慢，但可以遵循布局结果
    /// </summary>
    public bool UseRenderCharByCharMode { get; init; } = false;

    /// <summary>
    /// 字墨在字外框内的对齐方式
    /// </summary>
    public SkiaTextEditorCharRenderFaceInFrameAlignment RenderFaceInFrameAlignment { get; init; } = SkiaTextEditorCharRenderFaceInFrameAlignment.Left;
}

/// <summary>
/// 字墨在字外框内的对齐方式
/// </summary>
/// <remarks>仅横排生效，仅只在逐字渲染时才能生效</remarks>
public enum SkiaTextEditorCharRenderFaceInFrameAlignment : byte
{
    Left = 0,
    Center,
    Right,
}