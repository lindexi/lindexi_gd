using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class DualEditorUserControl : UserControl
{
    public DualEditorUserControl()
    {
        InitializeComponent();

        LeftTextEditor.SetStyleParagraphProperty(LeftTextEditor.StyleParagraphProperty with
        {
            ParagraphBefore = 30,
        });
        LeftTextEditor.TextEditorCore.DocumentChanged += LeftTextEditor_DocumentChanged;
        LeftTextEditor.TextEditorCore.LayoutCompleted += LeftTextEditor_LayoutCompleted;
        LeftTextEditor.SetFontSize(60);
        LeftTextEditor.AppendText("""
                                  江南可采莲，莲叶何田田，鱼戏莲叶间。
                                  鱼戏莲叶东，鱼戏莲叶西，
                                  鱼戏莲叶南，鱼戏莲叶北。
                                  """);

        RightTextEditor.SetStyleParagraphProperty(RightTextEditor.StyleParagraphProperty with
        {
            ParagraphBefore = 30,
        });
        RightTextEditor.TextEditorCore.DocumentChanged += RightTextEditor_DocumentChanged;
        RightTextEditor.TextEditorCore.LayoutCompleted += RightTextEditor_LayoutCompleted;
        RightTextEditor.SetFontSize(60);
        RightTextEditor.AppendText("""
                                   江南莲花开，莲花惹人采。莲叶一片绿，仿佛成碧海。鱼儿知戏乐，寻踪觅芳来。
                                   鱼儿畅游莲叶东，鱼儿畅游莲叶西，
                                   鱼儿畅游莲叶南，鱼儿畅游莲叶北。
                                   """);

        _isInitialized = true;

        var leftList = LeftTextEditor.TextEditorCore.ParagraphList;
        var rightList = RightTextEditor.TextEditorCore.ParagraphList;
        Debug.Assert(leftList.Count == rightList.Count);

        for (var i = 0; i < leftList.Count; i++)
        {
            _associatedParagraphList.Add((leftList[i], rightList[i]));
        }
    }

    private readonly List<(ITextParagraph Left, ITextParagraph Right)> _associatedParagraphList = [];

    private readonly bool _isInitialized;

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