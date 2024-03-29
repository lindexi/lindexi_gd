﻿using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Utils;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
///     提供平台接入的辅助类，可以减少编写的代码量
/// </summary>
public abstract class PlatformProvider : IPlatformProvider
{
    private DefaultRunParagraphSplitter? _defaultRunParagraphSplitter;

    public virtual IPlatformRunPropertyCreator GetPlatformRunPropertyCreator()
    {
        // 尽管有默认的实现，但是推荐还是有具体的平台实现逻辑
        return new DefaultPlatformRunPropertyCreator();
    }

    /// <inheritdoc />
    public virtual void RequireDispatchUpdateLayout(Action textLayout)
    {
        textLayout();
    }

    /// <inheritdoc />
    public virtual ITextLogger? BuildTextLogger()
    {
        return null;
    }

    /// <inheritdoc />
    public virtual IRunParagraphSplitter GetRunParagraphSplitter()
    {
        return _defaultRunParagraphSplitter ??= new DefaultRunParagraphSplitter();
    }

    /// <inheritdoc />
    public virtual IWholeLineLayouter? GetWholeRunLineLayouter()
    {
        return null;
    }

    //public virtual IRunMeasureProvider GetRunMeasureProvider()
    //{
    //    return  new DefaultRunMeasureProvider();
    //}

    /// <inheritdoc />
    public virtual ISingleCharInLineLayouter? GetSingleRunLineLayouter()
    {
        return null;
    }

    //public ISingleCharInLineLayouter? GetSingleCharInLineLayouter()
    //{
    //    return null;
    //}

    /// <inheritdoc />
    public virtual ICharInfoMeasurer? GetCharInfoMeasurer()
    {
        return null;
    }

    /// <inheritdoc />
    public virtual IRenderManager? GetRenderManager()
    {
        return null;
    }

    /// <inheritdoc />
    public virtual IEmptyParagraphLineHeightMeasurer? GetEmptyParagraphLineHeightMeasurer()
    {
        return null;
    }
}