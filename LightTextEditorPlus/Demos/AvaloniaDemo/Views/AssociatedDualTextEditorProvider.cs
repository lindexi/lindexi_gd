using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

/// <summary>
/// 提供关联的双文本编辑器的关联
/// </summary>
class AssociatedDualTextEditorProvider
{
    /// <summary>
    /// 创建提供关联的双文本编辑器的关联，前置要求是两个文本编辑器的文本属性设置是相同的，如相同的字号
    /// </summary>
    /// <param name="leftTextEditor"></param>
    /// <param name="rightTextEditor"></param>
    public AssociatedDualTextEditorProvider(TextEditor leftTextEditor, TextEditor rightTextEditor)
    {
        LeftTextEditor = leftTextEditor;
        RightTextEditor = rightTextEditor;

        LeftTextEditor.TextEditorCore.DocumentChanged += LeftTextEditor_DocumentChanged;
        LeftTextEditor.LayoutCompleted += LeftTextEditor_LayoutCompleted;

        RightTextEditor.TextEditorCore.DocumentChanged += RightTextEditor_DocumentChanged;
        RightTextEditor.LayoutCompleted += RightTextEditor_LayoutCompleted;
    }

    public TextEditor LeftTextEditor { get; }

    public TextEditor RightTextEditor { get; }

    public void SetInitialized() => _isInitialized = true;
    private bool _isInitialized;

    private void LeftTextEditor_LayoutCompleted(object? sender, LayoutCompletedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }
        UpdateDualEditorAlignment();
    }

    private void LeftTextEditor_DocumentChanged(object? sender, System.EventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }
    }

    private void RightTextEditor_LayoutCompleted(object? sender, LayoutCompletedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        UpdateDualEditorAlignment();
    }

    private void RightTextEditor_DocumentChanged(object? sender, System.EventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }
        else
        {

        }
    }

    private void UpdateDualEditorAlignment()
    {
        if (LeftTextEditor.IsDirty || RightTextEditor.IsDirty)
        {
            return;
        }

        var leftList = LeftTextEditor.TextEditorCore.ParagraphList;
        var rightList = RightTextEditor.TextEditorCore.ParagraphList;

        RenderInfoProvider leftRenderInfoProvider = LeftTextEditor.TextEditorCore.GetRenderInfo();
        RenderInfoProvider rightRenderInfoProvider = RightTextEditor.TextEditorCore.GetRenderInfo();

        List<ParagraphRenderInfo> leftParagraphRenderInfoList = leftRenderInfoProvider.GetParagraphRenderInfoList().ToList();
        List<ParagraphRenderInfo> rightParagraphRenderInfoList = rightRenderInfoProvider.GetParagraphRenderInfoList().ToList();

        Debug.Assert(leftList.Count == rightList.Count);
        Debug.Assert(leftList.Count == leftParagraphRenderInfoList.Count);
        Debug.Assert(rightList.Count == rightParagraphRenderInfoList.Count);

        for (var i = 0; i < leftList.Count; i++)
        {
            ITextParagraph leftParagraph = leftList[i];
            ITextParagraph rightParagraph = rightList[i];

            ParagraphRenderInfo leftParagraphRenderInfo = leftParagraphRenderInfoList[i];
            ParagraphRenderInfo rightParagraphRenderInfo = rightParagraphRenderInfoList[i];

            Debug.Assert(ReferenceEquals(leftParagraph, leftParagraphRenderInfo.Paragraph));
            Debug.Assert(ReferenceEquals(rightParagraph, rightParagraphRenderInfo.Paragraph));

            // 最小高度就是两个段落之间，看哪个段落的高度更高
            double leftParagraphHeight = leftParagraphRenderInfo.ParagraphLayoutData.TextSize.Height;
            double rightParagraphHeight = rightParagraphRenderInfo.ParagraphLayoutData.TextSize.Height;

            var minHeight = Math.Max(leftParagraphHeight, rightParagraphHeight);

            // 两个段落的高度不一致，那么就调整高度
            var leftGap = minHeight - leftParagraphHeight;
            var rightGap = minHeight - rightParagraphHeight;

            if (Math.Abs(leftParagraph.ParagraphProperty.ParagraphAfter - leftGap) > 0.01)
            {
                LeftTextEditor.TextEditorCore.DocumentManager.SetParagraphProperty(leftParagraph, leftParagraph.ParagraphProperty with
                {
                    ParagraphAfter = leftGap
                });
            }

            if (Math.Abs(rightParagraph.ParagraphProperty.ParagraphAfter - rightGap) > 0.01)
            {
                RightTextEditor.TextEditorCore.DocumentManager.SetParagraphProperty(rightParagraph, rightParagraph.ParagraphProperty with
                {
                    ParagraphAfter = rightGap
                });
            }
        }
    }
}