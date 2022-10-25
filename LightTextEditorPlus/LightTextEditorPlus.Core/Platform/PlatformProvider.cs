using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 提供平台接入的辅助类，可以减少编写的代码量
/// </summary>
public abstract class PlatformProvider : IPlatformProvider
{
    public virtual void RequireDispatchUpdateLayout(Action textLayout)
    {
        textLayout();
    }

    public virtual ITextLogger? BuildTextLogger()
    {
        return null;
    }

    public virtual IRunParagraphSplitter GetRunParagraphSplitter()
    {
        return _defaultRunParagraphSplitter ??= new DefaultRunParagraphSplitter();
    }

    public virtual IWholeLineLayouter? GetWholeRunLineLayouter()
    {
        return null;
    }

    //public virtual IRunMeasureProvider GetRunMeasureProvider()
    //{
    //    return  new DefaultRunMeasureProvider();
    //}

    public virtual ISingleCharInLineLayouter? GetSingleRunLineLayouter()
    {
        return null;
    }

    //public ISingleCharInLineLayouter? GetSingleCharInLineLayouter()
    //{
    //    return null;
    //}

    public virtual ICharInfoMeasurer? GetCharInfoMeasurer()
    {
        return null;
    }

    public virtual IRenderManager? GetRenderManager()
    {
        return null;
    }

    private DefaultRunParagraphSplitter? _defaultRunParagraphSplitter;
}