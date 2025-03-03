using System.Collections.Generic;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using LightTextEditorPlus.AvaloniaDemo.Views;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class DualEditorUserControl : UserControl
{
    public DualEditorUserControl()
    {
        InitializeComponent();

        _associatedDualTextEditorProvider = new AssociatedDualTextEditorProvider(LeftTextEditor, RightTextEditor);

        LeftTextEditor.SetStyleParagraphProperty(LeftTextEditor.StyleParagraphProperty with
        {
            ParagraphBefore = 30,
        });
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
        RightTextEditor.SetFontSize(60);
        RightTextEditor.AppendText("""
                                   江南莲花开，莲花惹人采。莲叶一片绿，仿佛成碧海。鱼儿知戏乐，寻踪觅芳来。
                                   鱼儿畅游莲叶东，鱼儿畅游莲叶西，
                                   鱼儿畅游莲叶南，鱼儿畅游莲叶北。
                                   """);

        _associatedDualTextEditorProvider.SetInitialized();

        var leftList = LeftTextEditor.TextEditorCore.ParagraphList;
        var rightList = RightTextEditor.TextEditorCore.ParagraphList;
        Debug.Assert(leftList.Count == rightList.Count);

        for (var i = 0; i < leftList.Count; i++)
        {
            _associatedParagraphList.Add((leftList[i], rightList[i]));
        }
    }

    private readonly List<(ITextParagraph Left, ITextParagraph Right)> _associatedParagraphList = [];

    private readonly AssociatedDualTextEditorProvider _associatedDualTextEditorProvider;
}