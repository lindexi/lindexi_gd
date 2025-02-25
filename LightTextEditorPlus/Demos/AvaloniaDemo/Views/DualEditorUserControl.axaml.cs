using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LightTextEditorPlus.Core.Events;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class DualEditorUserControl : UserControl
{
    public DualEditorUserControl()
    {
        InitializeComponent();
        
        LeftTextEditor.AppendText("""
                                  江南可采莲，莲叶何田田，鱼戏莲叶间。
                                  鱼戏莲叶东，鱼戏莲叶西，
                                  鱼戏莲叶南，鱼戏莲叶北。
                                  """);
        LeftTextEditor.SetFontSize(60);
        LeftTextEditor.TextEditorCore.DocumentChanged += LeftTextEditor_DocumentChanged;
        LeftTextEditor.TextEditorCore.LayoutCompleted += LeftTextEditor_LayoutCompleted;

        RightTextEditor.AppendText("""
                                   江南莲花开，莲花惹人采。莲叶一片绿，仿佛成碧海。鱼儿知戏乐，寻踪觅芳来。
                                   鱼儿畅游莲叶东，鱼儿畅游莲叶西，
                                   鱼儿畅游莲叶南，鱼儿畅游莲叶北。
                                   """);
        RightTextEditor.SetFontSize(60);
        RightTextEditor.TextEditorCore.DocumentChanged += RightTextEditor_DocumentChanged;
        RightTextEditor.TextEditorCore.LayoutCompleted += RightTextEditor_LayoutCompleted;
    }

    private void LeftTextEditor_LayoutCompleted(object? sender, LayoutCompletedEventArgs e)
    {
        //LeftTextEditor.TextEditorCore.DocumentManager.pa
    }

    private void LeftTextEditor_DocumentChanged(object? sender, System.EventArgs e)
    {
    }

    private void RightTextEditor_LayoutCompleted(object? sender, LayoutCompletedEventArgs e)
    {
    }

    private void RightTextEditor_DocumentChanged(object? sender, System.EventArgs e)
    {
    }
}