using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Layout;

class LayoutManager
{
    public LayoutManager(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditor TextEditor { get; }
    public event EventHandler? InternalLayoutCompleted;

    public void UpdateLayout()
    {
        if (ArrangingLayoutProvider?.ArrangingType != TextEditor.ArrangingType)
        {
            ArrangingLayoutProvider = TextEditor.ArrangingType switch
            {
                ArrangingType.Horizontal => new HorizontalArrangingLayoutProvider(TextEditor),
                // todo 支持竖排文本
                _ => throw new NotSupportedException()
            };
        }

        ArrangingLayoutProvider.UpdateLayout();

        InternalLayoutCompleted?.Invoke(this, EventArgs.Empty);
    }

    private ArrangingLayoutProvider? ArrangingLayoutProvider { set; get; }
}

/// <summary>
/// 水平方向布局的提供器
/// </summary>
class HorizontalArrangingLayoutProvider : ArrangingLayoutProvider
{
    public HorizontalArrangingLayoutProvider(TextEditorCore textEditor) : base(textEditor)
    {
    }

    public override ArrangingType ArrangingType => ArrangingType.Horizontal;

    protected override void LayoutParagraph(ParagraphData paragraph)
    {
        // todo 未更改的行，不需要重新布局更新


    }
}

/// <summary>
/// 实际的布局提供器
/// </summary>
abstract class ArrangingLayoutProvider
{
    protected ArrangingLayoutProvider(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    public abstract ArrangingType ArrangingType { get; }
    public TextEditorCore TextEditor { get; }

    public void UpdateLayout()
    {
        // todo 项目符号的段落，如果在段落上方新建段落，那需要项目符号更新
        // 这个逻辑准备给项目符号逻辑更新，逻辑是，假如现在有两段，分别采用 `1. 2.` 作为项目符号
        // 在 `1.` 后面新建一个段落，需要自动将原本的 `2.` 修改为 `3.` 的内容，这个逻辑准备交给项目符号模块自己编辑实现

        // 布局逻辑：
        // - 获取需要更新布局段落的逻辑
        // - 进入段落布局
        //   - 进入行布局
        // - 所有段落布局完成之后，再根据段落之间的高度信息，更新每个段落的 YOffset 的值

        // 首行出现变脏的序号
        var firstDirtyParagraphIndex = -1;
        var dirtyParagraphDataList = new List<ParagraphData>();
        var list = TextEditor.DocumentManager.TextRunManager.ParagraphManager.GetParagraphList();
        for (var index = 0; index < list.Count; index++)
        {
            ParagraphData paragraphData = list[index];
            if (paragraphData.IsDirty)
            {
                if (firstDirtyParagraphIndex == -1)
                {
                    firstDirtyParagraphIndex = index;
                }

                dirtyParagraphDataList.Add(paragraphData);
            }
        }

        // 进入段落内布局
        foreach (var paragraphData in dirtyParagraphDataList)
        {
            LayoutParagraph(paragraphData);
        }

        // todo 完成测量最大宽度
        var sizeToContent = TextEditor.SizeToContent;



        // 完成布局之后，全部设置为非脏的（或者是段落内自己实现）
        foreach (var paragraphData in dirtyParagraphDataList)
        {
            paragraphData.IsDirty = false;
        }

        Debug.Assert(TextEditor.DocumentManager.TextRunManager.ParagraphManager.GetParagraphList().All(t => t.IsDirty == false));
    }

    /// <summary>
    /// 段落内布局
    /// </summary>
    /// <param name="paragraph"></param>
    protected abstract void LayoutParagraph(ParagraphData paragraph);
}