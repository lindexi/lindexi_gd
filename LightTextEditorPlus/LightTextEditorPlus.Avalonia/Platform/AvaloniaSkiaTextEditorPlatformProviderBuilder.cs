﻿namespace LightTextEditorPlus.Platform;

/// <summary>
/// 文本库的平台提供者创建器
/// </summary>
public class AvaloniaSkiaTextEditorPlatformProviderBuilder : IAvaloniaSkiaTextEditorPlatformProviderBuilder
{
    /// <inheritdoc />
    public virtual AvaloniaSkiaTextEditorPlatformProvider Build(TextEditor avaloniaTextEditor)
    {
        return new AvaloniaSkiaTextEditorPlatformProvider(avaloniaTextEditor);
    }
}

/// <summary>
/// 文本库的平台提供者创建器
/// </summary>
public interface IAvaloniaSkiaTextEditorPlatformProviderBuilder
{
    /// <summary>
    /// 构建关联文本对象的文本库的平台提供者
    /// </summary>
    /// <param name="avaloniaTextEditor"></param>
    /// <returns></returns>
    AvaloniaSkiaTextEditorPlatformProvider Build(TextEditor avaloniaTextEditor);
}