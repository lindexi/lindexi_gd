using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Document.Utils;
using LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
///     提供平台接入的辅助类，可以减少编写的代码量
/// </summary>
public abstract class PlatformProvider : ITextEditorPlatformProvider
{
    /// <inheritdoc />
    public virtual ITextEditorUndoRedoProvider BuildTextEditorUndoRedoProvider()
    {
        return new TextEditorUndoRedoProvider();
    }

    /// <inheritdoc />
    public virtual IPlatformRunPropertyCreator GetPlatformRunPropertyCreator()
    {
        // 尽管有默认的实现，但是推荐还是有具体的平台实现逻辑
        return new DefaultPlatformRunPropertyCreator();
    }

    /// <inheritdoc />
    public virtual void RequireDispatchUpdateLayout(Action updateLayoutAction)
    {
        updateLayoutAction();
    }

    /// <inheritdoc />
    public virtual void InvokeDispatchUpdateLayout(Action updateLayoutAction)
    {
        // 由于默认实现在 RequireDispatchUpdateLayout 是立刻执行，因此可以忽略清理
        updateLayoutAction();
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
    private DefaultRunParagraphSplitter? _defaultRunParagraphSplitter;

    /// <inheritdoc />
    public virtual IWholeLineLayouter? GetWholeRunLineLayouter()
    {
        return null;
    }

    /// <inheritdoc />
    public virtual IWholeLineCharsLayouter? GetWholeLineCharsLayouter() => null;

    //public virtual IRunMeasureProvider GetRunMeasureProvider()
    //{
    //    return  new DefaultRunMeasureProvider();
    //}

    /// <inheritdoc />
    public virtual ISingleCharInLineLayouter? GetSingleRunLineLayouter()
    {
        return null;
    }

    //public virtual ISingleCharInLineLayouter? GetSingleCharInLineLayouter()
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
    public virtual double GetFontLineSpacing(IReadOnlyRunProperty runProperty)
    {
        // 默认是 1 行距。各个平台可以自行计算
        return 1;
    }

    /// <inheritdoc />
    public virtual IEmptyParagraphLineHeightMeasurer? GetEmptyParagraphLineHeightMeasurer()
    {
        return null;
    }

    /// <inheritdoc />
    public virtual ILineSpacingCalculator? GetLineSpacingCalculator()
    {
        return null;
    }

    /// <inheritdoc />
    public virtual IPlatformFontNameManager GetPlatformFontNameManager()
    {
        return new DefaultPlatformFontNameManager();
    }

    /// <inheritdoc />
    public virtual IWordDivider GetWordDivider()
    {
        // 分词器可以考虑是注入的，而不是平台相关的
        return _wordDivider ??= new WordDivider();
    }

    private WordDivider? _wordDivider;
}
